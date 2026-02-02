// ============== small utilities ==============
const $ = (id) => document.getElementById(id);

function nowTs() {
  return new Date().toISOString();
}

function log(msg, obj) {
  const el = $("log");
  const prefix = `[${nowTs()}] `;
  if (obj !== undefined) {
    el.textContent += `${prefix}${msg}\n${JSON.stringify(obj, null, 2)}\n\n`;
  } else {
    el.textContent += `${prefix}${msg}\n\n`;
  }
  el.scrollTop = el.scrollHeight;
}

function clearLog() { $("log").textContent = ""; }

function getBaseUrl() {
  const saved = localStorage.getItem("poc_locations_baseUrl");
  const v = saved || $("baseUrl").value.trim();
  return v.replace(/\/+$/, "");
}

function setBaseUrl(v) {
  const clean = v.trim().replace(/\/+$/, "");
  localStorage.setItem("poc_locations_baseUrl", clean);
  $("baseUrl").value = clean;
}

async function apiFetch(path, { method="GET", body=null, headers={} } = {}) {
  const url = `${getBaseUrl()}${path}`;
  const opts = {
    method,
    headers: {
      "Accept": "application/json",
      ...headers
    }
  };
  if (body !== null) {
    opts.headers["Content-Type"] = "application/json";
    opts.body = JSON.stringify(body);
  }
  log(`→ ${method} ${url}`, body ?? undefined);

  const res = await fetch(url, opts);
  const contentType = res.headers.get("content-type") || "";
  let payload = null;

  if (contentType.includes("application/json")) {
    payload = await res.json().catch(() => null);
  } else {
    payload = await res.text().catch(() => null);
  }

  if (!res.ok) {
    log(`✗ ${res.status} ${res.statusText}`, payload);
    throw new Error(`HTTP ${res.status}: ${res.statusText}`);
  }

  log(`✓ ${res.status} ${res.statusText}`, payload);
  return payload;
}

function parseCsvList(s) {
  if (!s || !s.trim()) return null;
  return s.split(",").map(x => x.trim()).filter(Boolean);
}

function clampInt(n, min, max, fallback) {
  const x = Number.parseInt(n, 10);
  if (Number.isNaN(x)) return fallback;
  return Math.max(min, Math.min(max, x));
}

// ============== Tabs ==============
function initTabs() {
  $("tabs").addEventListener("click", (e) => {
    const btn = e.target.closest(".tab");
    if (!btn) return;

    document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));
    btn.classList.add("active");

    const tab = btn.dataset.tab;
    document.querySelectorAll(".tabPanel").forEach(p => p.classList.add("hidden"));
    $(`tab-${tab}`).classList.remove("hidden");
  });
}

// ============== Health / DB ping ==============
async function initHeaderActions() {
  $("btnSaveBaseUrl").addEventListener("click", () => {
    setBaseUrl($("baseUrl").value);
    log("Saved base URL", { baseUrl: getBaseUrl() });
  });

  $("btnHealth").addEventListener("click", async () => {
    try { await apiFetch("/v1/health"); } catch (e) { log("Health failed", { error: e.message }); }
  });

  $("btnDbPing").addEventListener("click", async () => {
    try { await apiFetch("/v1/db-ping"); } catch (e) { log("DB ping failed", { error: e.message }); }
  });

  $("btnClearLog").addEventListener("click", clearLog);

  const saved = localStorage.getItem("poc_locations_baseUrl");
  if (saved) $("baseUrl").value = saved;
}

// ============== Maps ==============
function renderMapsTable(items) {
  const tbody = $("mapsTable").querySelector("tbody");
  tbody.innerHTML = "";
  for (const m of items) {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(m.mapId ?? m.map_id ?? "")}</td>
      <td>${escapeHtml(m.name ?? "")}</td>
      <td>${escapeHtml(m.status ?? "")}</td>
      <td>${escapeHtml(m.updatedAt ?? m.updated_at ?? "")}</td>
    `;
    tr.addEventListener("click", () => {
      const id = m.mapId ?? m.map_id;
      $("mapEditId").value = id || "";
      $("mapEditName").value = m.name || "";
      $("mapEditDesc").value = m.description || "";
      $("mapEditStatus").value = (m.status || "ACTIVE").toUpperCase();
      log("Loaded map into editor", { map_id: id });
    });
    tbody.appendChild(tr);
  }
}

async function loadMaps() {
  const status = $("mapsStatus").value;
  const data = await apiFetch(`/v1/maps?status=${encodeURIComponent(status)}`);
  renderMapsTable(data.data || []);
}

async function createMap() {
  const body = {
    mapId: $("mapCreateId").value.trim(),
    name: $("mapCreateName").value.trim(),
    description: $("mapCreateDesc").value.trim() || null
  };
  await apiFetch("/v1/admin/maps", { method: "POST", body });
  await loadMaps();
}

async function getMap() {
  const id = $("mapEditId").value.trim();
  if (!id) return;
  const m = await apiFetch(`/v1/admin/maps/${encodeURIComponent(id)}`);
  $("mapEditName").value = m.name || "";
  $("mapEditDesc").value = m.description || "";
  $("mapEditStatus").value = (m.status || "ACTIVE").toUpperCase();
}

async function patchMap() {
  const id = $("mapEditId").value.trim();
  if (!id) return;

  const body = {
    name: $("mapEditName").value.trim() || null,
    description: $("mapEditDesc").value.trim(), // allow empty string if you want clear
    status: $("mapEditStatus").value
  };
  await apiFetch(`/v1/admin/maps/${encodeURIComponent(id)}`, { method: "PATCH", body });
  await loadMaps();
}

function initMaps() {
  $("btnLoadMaps").addEventListener("click", () => loadMaps().catch(e => log("Load maps failed", { error: e.message })));
  $("btnCreateMap").addEventListener("click", () => createMap().catch(e => log("Create map failed", { error: e.message })));
  $("btnGetMap").addEventListener("click", () => getMap().catch(e => log("Get map failed", { error: e.message })));
  $("btnPatchMap").addEventListener("click", () => patchMap().catch(e => log("Patch map failed", { error: e.message })));
}

// ============== Countries ==============
function renderCountriesTable(items) {
  const tbody = $("countriesTable").querySelector("tbody");
  tbody.innerHTML = "";
  for (const c of items) {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(c.countryId ?? c.country_id ?? "")}</td>
      <td>${escapeHtml(c.name ?? "")}</td>
      <td>${escapeHtml(c.status ?? "")}</td>
      <td>${escapeHtml(c.updatedAt ?? c.updated_at ?? "")}</td>
    `;
    tr.addEventListener("click", () => {
      const id = c.countryId ?? c.country_id;
      $("countryEditId").value = id || "";
      $("countryEditName").value = c.name || "";
      $("countryEditStatus").value = (c.status || "ACTIVE").toUpperCase();
      log("Loaded country into editor", { country_id: id });
    });
    tbody.appendChild(tr);
  }
}

async function loadCountries() {
  const status = $("countriesStatus").value;
  const data = await apiFetch(`/v1/countries?status=${encodeURIComponent(status)}`);
  renderCountriesTable(data.data || []);
}

async function createCountry() {
  const body = {
    countryId: $("countryCreateId").value.trim(),
    name: $("countryCreateName").value.trim()
  };
  await apiFetch("/v1/admin/countries", { method: "POST", body });
  await loadCountries();
}

async function getCountry() {
  const id = $("countryEditId").value.trim();
  if (!id) return;
  const c = await apiFetch(`/v1/admin/countries/${encodeURIComponent(id)}`);
  $("countryEditName").value = c.name || "";
  $("countryEditStatus").value = (c.status || "ACTIVE").toUpperCase();
}

async function patchCountry() {
  const id = $("countryEditId").value.trim();
  if (!id) return;
  const body = {
    name: $("countryEditName").value.trim() || null,
    status: $("countryEditStatus").value
  };
  await apiFetch(`/v1/admin/countries/${encodeURIComponent(id)}`, { method: "PATCH", body });
  await loadCountries();
}

function initCountries() {
  $("btnLoadCountries").addEventListener("click", () => loadCountries().catch(e => log("Load countries failed", { error: e.message })));
  $("btnCreateCountry").addEventListener("click", () => createCountry().catch(e => log("Create country failed", { error: e.message })));
  $("btnGetCountry").addEventListener("click", () => getCountry().catch(e => log("Get country failed", { error: e.message })));
  $("btnPatchCountry").addEventListener("click", () => patchCountry().catch(e => log("Patch country failed", { error: e.message })));
}

// ============== Locations (Admin) ==============
function renderLocationsTable(items) {
  const tbody = $("locationsTable").querySelector("tbody");
  tbody.innerHTML = "";
  for (const l of items) {
    const tr = document.createElement("tr");
    const id = l.locationId ?? l.location_id ?? "";
    tr.innerHTML = `
      <td>${escapeHtml(id)}</td>
      <td>${escapeHtml(l.externalReference ?? l.external_reference ?? "")}</td>
      <td>${escapeHtml(l.name ?? "")}</td>
      <td>${escapeHtml(l.city ?? "")}</td>
      <td>${escapeHtml(l.region ?? "")}</td>
      <td>${escapeHtml(String(l.latitude ?? ""))}</td>
      <td>${escapeHtml(String(l.longitude ?? ""))}</td>
      <td>${escapeHtml(l.status ?? "")}</td>
      <td>${escapeHtml(l.updatedAt ?? l.updated_at ?? "")}</td>
    `;
    tr.addEventListener("click", async () => {
      $("locEditId").value = id;
      log("Selected location_id for edit", { location_id: id });
    });
    tbody.appendChild(tr);
  }
}

async function loadLocations() {
  const status = $("locStatus").value;
  const mapId = $("locMapId").value.trim();
  const countryId = $("locCountryId").value.trim();
  const updatedSince = $("locUpdatedSince").value.trim();

  const limit = clampInt($("locLimit").value, 1, 5000, 50);
  const offset = clampInt($("locOffset").value, 0, 1_000_000, 0);

  const qs = new URLSearchParams();
  qs.set("status", status);
  qs.set("limit", String(limit));
  qs.set("offset", String(offset));
  if (mapId) qs.set("map_id", mapId);
  if (countryId) qs.set("country_id", countryId);
  if (updatedSince) qs.set("updated_since", updatedSince);

  const data = await apiFetch(`/v1/admin/locations?${qs.toString()}`);
  renderLocationsTable(data.data || []);
}

async function loadLocationIds() {
  const status = $("locStatus").value;
  const limit = 5000;
  const offset = 0;
  const data = await apiFetch(`/v1/admin/locations/ids?status=${encodeURIComponent(status)}&limit=${limit}&offset=${offset}`);
  const ids = (data.data || []).map(String);
  $("locIdsBox").value = ids.join("\n");
}

async function getLocation() {
  const id = $("locEditId").value.trim();
  if (!id) return;
  const loc = await apiFetch(`/v1/admin/locations/${encodeURIComponent(id)}`);
  // Fill some editable fields (only those we have inputs for)
  $("locEditStatus").value = "";
  $("locEditName").value = loc.name ?? "";
  $("locEditAddr1").value = loc.address_line1 ?? loc.addressLine1 ?? "";
  $("locEditCity").value = loc.city ?? "";
  $("locEditRegion").value = loc.region ?? "";
  $("locEditPostal").value = loc.postal_code ?? loc.postalCode ?? "";
  $("locEditLat").value = (loc.latitude ?? "").toString();
  $("locEditLng").value = (loc.longitude ?? "").toString();
}

async function createLocation() {
  const body = {
    externalReference: $("locCreateExt").value.trim() || null,
    name: $("locCreateName").value.trim(),
    addressLine1: $("locCreateAddr1").value.trim(),
    addressLine2: $("locCreateAddr2").value.trim() || null,
    city: $("locCreateCity").value.trim(),
    region: $("locCreateRegion").value.trim() || null,
    postalCode: $("locCreatePostal").value.trim() || null,
    latitude: Number($("locCreateLat").value),
    longitude: Number($("locCreateLng").value),
    phone: $("locCreatePhone").value.trim() || null,
    email: $("locCreateEmail").value.trim() || null,
    websiteUrl: $("locCreateUrl").value.trim() || null,
    hoursText: $("locCreateHours").value.trim() || null,
    countries: parseCsvList($("locCreateCountries").value),
    maps: parseCsvList($("locCreateMaps").value)
  };

  if (!body.name || !body.addressLine1 || !body.city || Number.isNaN(body.latitude) || Number.isNaN(body.longitude)) {
    throw new Error("Missing required fields or invalid lat/lng.");
  }

  await apiFetch("/v1/admin/locations", { method: "POST", body });
  await loadLocations();
}

async function patchLocation() {
  const id = $("locEditId").value.trim();
  if (!id) return;

  const status = $("locEditStatus").value;
  const name = $("locEditName").value.trim();
  const addr1 = $("locEditAddr1").value.trim();
  const city = $("locEditCity").value.trim();
  const region = $("locEditRegion").value.trim();
  const postal = $("locEditPostal").value.trim();
  const latRaw = $("locEditLat").value.trim();
  const lngRaw = $("locEditLng").value.trim();

  const body = {
    status: status || null,
    name: name || null,
    addressLine1: addr1 || null,
    city: city || null,
    region: region || null,
    postalCode: postal || null,
    latitude: latRaw ? Number(latRaw) : null,
    longitude: lngRaw ? Number(lngRaw) : null
  };

  // If only one of lat/lng is provided, avoid sending partial update (your API expects both to update geom)
  const latProvided = body.latitude !== null && !Number.isNaN(body.latitude);
  const lngProvided = body.longitude !== null && !Number.isNaN(body.longitude);
  if (latProvided !== lngProvided) {
    throw new Error("To update coordinates, provide BOTH latitude and longitude.");
  }
  if (body.latitude !== null && Number.isNaN(body.latitude)) body.latitude = null;
  if (body.longitude !== null && Number.isNaN(body.longitude)) body.longitude = null;

  await apiFetch(`/v1/admin/locations/${encodeURIComponent(id)}`, { method: "PATCH", body });
  await loadLocations();
}

function initLocations() {
  $("btnLoadLocations").addEventListener("click", () => loadLocations().catch(e => log("Load locations failed", { error: e.message })));
  $("btnLoadLocationIds").addEventListener("click", () => loadLocationIds().catch(e => log("Get location ids failed", { error: e.message })));
  $("btnGetLocation").addEventListener("click", () => getLocation().catch(e => log("Get location failed", { error: e.message })));
  $("btnCreateLocation").addEventListener("click", () => createLocation().catch(e => log("Create location failed", { error: e.message })));
  $("btnPatchLocation").addEventListener("click", () => patchLocation().catch(e => log("Patch location failed", { error: e.message })));
}

// ============== Public Geo Query ==============
function renderQueryTable(items) {
  const tbody = $("queryTable").querySelector("tbody");
  tbody.innerHTML = "";
  for (const l of items) {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(l.location_id ?? l.locationId ?? "")}</td>
      <td>${escapeHtml(l.name ?? "")}</td>
      <td>${escapeHtml(l.city ?? "")}</td>
      <td>${escapeHtml(l.region ?? "")}</td>
      <td>${escapeHtml(String(l.latitude ?? ""))}</td>
      <td>${escapeHtml(String(l.longitude ?? ""))}</td>
      <td>${escapeHtml((l.maps || []).join(","))}</td>
      <td>${escapeHtml((l.countries || []).join(","))}</td>
    `;
    tr.addEventListener("click", () => {
      const lat = l.latitude;
      const lng = l.longitude;
      if (lat != null && lng != null) {
        const url = `https://www.google.com/maps?q=${encodeURIComponent(lat)},${encodeURIComponent(lng)}`;
        window.open(url, "_blank");
      }
    });
    tbody.appendChild(tr);
  }
}

async function runQuery() {
  const mapId = $("qMapId").value.trim();
  const countryId = $("qCountryId").value.trim();
  const mode = document.querySelector('input[name="qmode"]:checked').value;

  const limit = clampInt($("qLimit").value, 1, 5000, 100);
  const offset = clampInt($("qOffset").value, 0, 1_000_000, 0);

  const qs = new URLSearchParams();
  if (mapId) qs.set("map_id", mapId);
  if (countryId) qs.set("country_id", countryId);
  qs.set("limit", String(limit));
  qs.set("offset", String(offset));

  if (mode === "bbox") {
    const bbox = $("qBbox").value.trim();
    if (!bbox) throw new Error("bbox is required in bbox mode.");
    qs.set("bbox", bbox);
  } else {
    const lat = $("qLat").value.trim();
    const lng = $("qLng").value.trim();
    const radius = $("qRadius").value.trim();
    if (!lat || !lng || !radius) throw new Error("lat, lng and radius_km are required in radius mode.");
    qs.set("lat", lat);
    qs.set("lng", lng);
    qs.set("radius_km", radius);
  }

  const data = await apiFetch(`/v1/locations?${qs.toString()}`);
  renderQueryTable(data.data || []);
}

function initQuery() {
  $("btnRunQuery").addEventListener("click", () => runQuery().catch(e => log("Query failed", { error: e.message })));
}

// ============== Bulk Associate ==============
function parseBulkItems(text, defaultStatus) {
  const lines = (text || "").split("\n").map(x => x.trim()).filter(Boolean);
  const items = [];
  for (const line of lines) {
    const [idRaw, statusRaw] = line.split(",").map(x => (x || "").trim());
    if (!idRaw) continue;
    items.push({
      locationId: idRaw,
      status: (statusRaw || defaultStatus || "ACTIVE").toUpperCase()
    });
  }
  return items;
}

async function runBulk() {
  const type = $("bulkType").value; // maps / countries
  const targetId = $("bulkTargetId").value.trim();
  const mode = $("bulkMode").value;
  const defaultStatus = $("bulkDefaultStatus").value;
  const items = parseBulkItems($("bulkItems").value, defaultStatus);

  if (!targetId) throw new Error("Target ID is required.");
  if (!items.length) throw new Error("Bulk items are empty.");

  const body = { mode, items: items.map(x => ({ locationId: x.locationId, status: x.status })) };

  const path =
    type === "maps"
      ? `/v1/admin/maps/${encodeURIComponent(targetId)}/locations:bulkAssociate`
      : `/v1/admin/countries/${encodeURIComponent(targetId)}/locations:bulkAssociate`;

  await apiFetch(path, { method: "POST", body });
}

function initBulk() {
  $("btnRunBulk").addEventListener("click", () => runBulk().catch(e => log("Bulk associate failed", { error: e.message })));

  $("btnBulkHelp").addEventListener("click", () => {
    const example = [
      "4e39a813-6f0b-4253-8f8a-0c1063c08366,ACTIVE",
      "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee,INACTIVE",
      "ffffffff-1111-2222-3333-444444444444"
    ].join("\n");
    $("bulkItems").value = example;
    log("Bulk example loaded");
  });
}

// ============== HTML escaping ==============
function escapeHtml(str) {
  return String(str ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

// ============== init ==============
(function init(){
  initTabs();
  initHeaderActions();
  initMaps();
  initCountries();
  initLocations();
  initQuery();
  initBulk();

  log("UI ready. Set API Base URL and run /v1/db-ping.");
})();
