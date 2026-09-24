import Keycloak from "keycloak-js";
import {
  ArrowDownLeft,
  ArrowRight,
  ArrowUpRight,
  BookUser,
  Building2,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  CircleDollarSign,
  Clock3,
  LayoutDashboard,
  LogOut,
  Plus,
  RefreshCw,
  Send,
  Trash2,
  UserRound,
  createIcons
} from "lucide";
import "./styles.css";

const keycloakBaseUrl = `${window.location.protocol}//${window.location.hostname}:8081`;
const keycloak = new Keycloak({
  url: keycloakBaseUrl,
  realm: "securebank",
  clientId: "securebank-web"
});

const icons = {
  ArrowDownLeft,
  ArrowRight,
  ArrowUpRight,
  BookUser,
  Building2,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  CircleDollarSign,
  Clock3,
  LayoutDashboard,
  LogOut,
  Plus,
  RefreshCw,
  Send,
  Trash2,
  UserRound
};

const state = {
  view: "overview",
  user: null,
  accounts: [],
  beneficiaries: [],
  transfers: emptyPage(),
  transferPage: 1
};

const app = document.querySelector("#app");

function emptyPage() {
  return {
    page: 1,
    pageSize: 25,
    totalItems: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
    items: []
  };
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatMoney(amount, currency = "EUR") {
  return new Intl.NumberFormat("en-IE", {
    style: "currency",
    currency,
    minimumFractionDigits: 2
  }).format(amount);
}

function formatDate(value) {
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

function shortAccount(accountNumber) {
  return `${accountNumber.slice(0, 6)} ${accountNumber.slice(6, 10)} ${accountNumber.slice(-6)}`;
}

async function api(path, options = {}) {
  await keycloak.updateToken(30);

  const response = await fetch(path, {
    ...options,
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${keycloak.token}`,
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...options.headers
    }
  });

  if (response.status === 401) {
    await keycloak.login();
    throw new Error("Your session expired.");
  }

  const contentType = response.headers.get("content-type") ?? "";
  const payload = contentType.includes("application/json")
    ? await response.json()
    : null;

  if (!response.ok) {
    const detail = payload?.detail
      ?? payload?.title
      ?? payload?.[0]
      ?? `Request failed with status ${response.status}.`;
    throw new Error(detail);
  }

  return payload;
}

async function loadData() {
  const [user, accounts, beneficiaries, transfers] = await Promise.all([
    api("/api/auth/me"),
    api("/api/accounts"),
    api("/api/beneficiaries"),
    api("/api/transfers?page=1&pageSize=25&sortBy=createdAt&sortDirection=desc")
  ]);

  state.user = user;
  state.accounts = accounts;
  state.beneficiaries = beneficiaries;
  state.transfers = transfers;
  state.transferPage = transfers.page;
}

function renderShell() {
  app.innerHTML = `
    <div class="app-shell">
      <aside class="sidebar">
        <div class="brand">
          <span class="brand-mark">SB</span>
          <span>SecureBank</span>
        </div>
        <nav class="main-nav" aria-label="Main navigation">
          ${navButton("overview", "LayoutDashboard", "Overview")}
          ${navButton("transfer", "Send", "New transfer")}
          ${navButton("beneficiaries", "BookUser", "Beneficiaries")}
          ${navButton("activity", "Clock3", "Activity")}
        </nav>
        <div class="sidebar-user">
          <div class="user-avatar"><i data-lucide="UserRound"></i></div>
          <div>
            <strong>${escapeHtml(state.user?.username ?? "Customer")}</strong>
            <span>${escapeHtml(state.user?.email ?? "")}</span>
          </div>
          <button class="icon-button" id="logout-button" title="Sign out" aria-label="Sign out">
            <i data-lucide="LogOut"></i>
          </button>
        </div>
      </aside>
      <main class="main-content">
        <header class="topbar">
          <div>
            <p class="eyebrow">Personal banking</p>
            <h1 id="page-title"></h1>
          </div>
          <button class="icon-button" id="refresh-button" title="Refresh data" aria-label="Refresh data">
            <i data-lucide="RefreshCw"></i>
          </button>
        </header>
        <section id="view-root" class="view-root"></section>
      </main>
    </div>`;

  bindShellEvents();
  renderView();
}

function navButton(view, icon, label) {
  return `
    <button class="nav-button" data-view="${view}">
      <i data-lucide="${icon}"></i>
      <span>${label}</span>
    </button>`;
}

function bindShellEvents() {
  document.querySelectorAll("[data-view]").forEach(button => {
    button.addEventListener("click", () => {
      state.view = button.dataset.view;
      renderView();
    });
  });

  document.querySelector("#logout-button").addEventListener("click", () => {
    keycloak.logout({ redirectUri: window.location.origin });
  });

  document.querySelector("#refresh-button").addEventListener("click", refreshAll);
}

function renderView() {
  document.querySelectorAll("[data-view]").forEach(button => {
    button.classList.toggle("active", button.dataset.view === state.view);
  });

  const title = {
    overview: "Overview",
    transfer: "New transfer",
    beneficiaries: "Beneficiaries",
    activity: "Account activity"
  }[state.view];

  document.querySelector("#page-title").textContent = title;

  if (state.view === "overview") renderOverview();
  if (state.view === "transfer") renderTransfer();
  if (state.view === "beneficiaries") renderBeneficiaries();
  if (state.view === "activity") renderActivity();

  createIcons({ icons });
}

function renderOverview() {
  const totalBalance = state.accounts.reduce((total, account) => total + account.balance, 0);
  const recent = state.transfers.items.slice(0, 5);

  document.querySelector("#view-root").innerHTML = `
    <div class="summary-strip">
      <div>
        <span>Total balance</span>
        <strong>${formatMoney(totalBalance)}</strong>
      </div>
      <div>
        <span>Active accounts</span>
        <strong>${state.accounts.filter(account => account.status === "Active").length}</strong>
      </div>
      <div>
        <span>Saved beneficiaries</span>
        <strong>${state.beneficiaries.length}</strong>
      </div>
    </div>
    <div class="section-heading">
      <h2>Your accounts</h2>
      <button class="text-button" data-go="transfer">Make a transfer <i data-lucide="ArrowRight"></i></button>
    </div>
    <div class="account-grid">
      ${state.accounts.length ? state.accounts.map(accountCard).join("") : emptyState("Building2", "No accounts available")}
    </div>
    <div class="section-heading">
      <h2>Recent activity</h2>
      <button class="text-button" data-go="activity">View all <i data-lucide="ArrowRight"></i></button>
    </div>
    ${renderTransferTable(recent)}`;

  bindGoButtons();
}

function accountCard(account) {
  return `
    <article class="account-card">
      <div class="account-card-top">
        <span class="account-icon"><i data-lucide="CircleDollarSign"></i></span>
        <span class="status status-${account.status.toLowerCase()}">${escapeHtml(account.status)}</span>
      </div>
      <p>Current account</p>
      <h3>${formatMoney(account.balance, account.currency)}</h3>
      <span class="account-number">${escapeHtml(shortAccount(account.accountNumber))}</span>
    </article>`;
}

function renderTransfer() {
  const activeAccounts = state.accounts.filter(account => account.status === "Active");

  document.querySelector("#view-root").innerHTML = `
    <div class="form-layout">
      <form id="transfer-form" class="form-panel">
        <div class="field">
          <label for="source-account">From account</label>
          <select id="source-account" name="sourceAccountId" required>
            ${activeAccounts.map(account => `
              <option value="${account.id}" data-currency="${account.currency}">
                ${escapeHtml(shortAccount(account.accountNumber))} · ${formatMoney(account.balance, account.currency)}
              </option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label for="saved-beneficiary">Saved beneficiary</label>
          <select id="saved-beneficiary">
            <option value="">Enter an account ID manually</option>
            ${state.beneficiaries.map(item => `
              <option value="${item.accountId}">${escapeHtml(item.nickname)}</option>`).join("")}
          </select>
        </div>
        <div class="field">
          <label for="destination-account">Destination account ID</label>
          <input id="destination-account" name="destinationAccountId" type="text" required autocomplete="off" />
        </div>
        <div class="field-row">
          <div class="field">
            <label for="amount">Amount</label>
            <input id="amount" name="amount" type="number" min="0.01" step="0.01" required />
          </div>
          <div class="field compact-field">
            <label for="currency">Currency</label>
            <input id="currency" name="currency" value="${activeAccounts[0]?.currency ?? "EUR"}" readonly />
          </div>
        </div>
        <button class="primary-button" type="submit" ${activeAccounts.length ? "" : "disabled"}>
          <i data-lucide="Send"></i> Send transfer
        </button>
      </form>
      <aside class="balance-panel">
        <span>Available balance</span>
        <strong id="available-balance">${activeAccounts[0] ? formatMoney(activeAccounts[0].balance, activeAccounts[0].currency) : "—"}</strong>
        <p>Transfers are processed immediately in this development environment.</p>
      </aside>
    </div>`;

  const source = document.querySelector("#source-account");
  const beneficiary = document.querySelector("#saved-beneficiary");
  const destination = document.querySelector("#destination-account");

  source?.addEventListener("change", () => {
    const account = state.accounts.find(item => item.id === source.value);
    document.querySelector("#currency").value = account?.currency ?? "EUR";
    document.querySelector("#available-balance").textContent = account
      ? formatMoney(account.balance, account.currency)
      : "—";
  });

  beneficiary.addEventListener("change", () => {
    if (beneficiary.value) destination.value = beneficiary.value;
  });

  document.querySelector("#transfer-form").addEventListener("submit", submitTransfer);
}

async function submitTransfer(event) {
  event.preventDefault();
  const button = event.currentTarget.querySelector("button[type='submit']");
  const data = new FormData(event.currentTarget);

  setButtonBusy(button, true);

  try {
    await api("/api/transfers", {
      method: "POST",
      headers: { "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify({
        sourceAccountId: data.get("sourceAccountId"),
        destinationAccountId: data.get("destinationAccountId"),
        amount: Number(data.get("amount")),
        currency: data.get("currency")
      })
    });

    showToast("Transfer completed.", "success");
    await refreshAll("overview");
  } catch (error) {
    showToast(error.message, "error");
  } finally {
    setButtonBusy(button, false);
  }
}

function renderBeneficiaries() {
  document.querySelector("#view-root").innerHTML = `
    <div class="beneficiary-layout">
      <form id="beneficiary-form" class="form-panel compact-form">
        <div class="section-heading form-heading">
          <h2>Add beneficiary</h2>
        </div>
        <div class="field">
          <label for="beneficiary-name">Nickname</label>
          <input id="beneficiary-name" name="nickname" maxlength="100" required />
        </div>
        <div class="field">
          <label for="beneficiary-account">Account ID</label>
          <input id="beneficiary-account" name="accountId" required />
        </div>
        <button class="primary-button" type="submit"><i data-lucide="Plus"></i> Add beneficiary</button>
      </form>
      <div class="beneficiary-list">
        ${state.beneficiaries.length
          ? state.beneficiaries.map(beneficiaryRow).join("")
          : emptyState("BookUser", "No saved beneficiaries")}
      </div>
    </div>`;

  document.querySelector("#beneficiary-form").addEventListener("submit", submitBeneficiary);
  document.querySelectorAll("[data-delete-beneficiary]").forEach(button => {
    button.addEventListener("click", () => deleteBeneficiary(button.dataset.deleteBeneficiary));
  });
}

function beneficiaryRow(item) {
  return `
    <div class="beneficiary-row">
      <span class="beneficiary-avatar">${escapeHtml(item.nickname.slice(0, 2).toUpperCase())}</span>
      <div>
        <strong>${escapeHtml(item.nickname)}</strong>
        <span>${escapeHtml(item.accountId)}</span>
      </div>
      <button class="icon-button danger" data-delete-beneficiary="${item.id}" title="Delete beneficiary" aria-label="Delete ${escapeHtml(item.nickname)}">
        <i data-lucide="Trash2"></i>
      </button>
    </div>`;
}

async function submitBeneficiary(event) {
  event.preventDefault();
  const button = event.currentTarget.querySelector("button[type='submit']");
  const data = new FormData(event.currentTarget);
  setButtonBusy(button, true);

  try {
    await api("/api/beneficiaries", {
      method: "POST",
      body: JSON.stringify({
        accountId: data.get("accountId"),
        nickname: data.get("nickname")
      })
    });
    showToast("Beneficiary added.", "success");
    await refreshAll("beneficiaries");
  } catch (error) {
    showToast(error.message, "error");
  } finally {
    setButtonBusy(button, false);
  }
}

async function deleteBeneficiary(id) {
  try {
    await api(`/api/beneficiaries/${id}`, { method: "DELETE" });
    showToast("Beneficiary removed.", "success");
    await refreshAll("beneficiaries");
  } catch (error) {
    showToast(error.message, "error");
  }
}

function renderActivity() {
  document.querySelector("#view-root").innerHTML = `
    <div class="table-toolbar">
      <select id="activity-account" aria-label="Filter by account">
        <option value="">All accounts</option>
        ${state.accounts.map(account => `<option value="${account.id}">${escapeHtml(shortAccount(account.accountNumber))}</option>`).join("")}
      </select>
      <select id="activity-status" aria-label="Filter by status">
        <option value="">All statuses</option>
        <option value="Completed">Completed</option>
        <option value="Pending">Pending</option>
        <option value="Failed">Failed</option>
      </select>
      <select id="activity-sort" aria-label="Sort activity">
        <option value="createdAt:desc">Newest first</option>
        <option value="createdAt:asc">Oldest first</option>
        <option value="amount:desc">Amount: high to low</option>
        <option value="amount:asc">Amount: low to high</option>
      </select>
    </div>
    <div id="activity-table">${renderTransferTable(state.transfers.items)}</div>
    ${renderPagination()}`;

  ["activity-account", "activity-status", "activity-sort"].forEach(id => {
    document.querySelector(`#${id}`).addEventListener("change", () => loadTransfers(1));
  });
  bindPagination();
}

function renderTransferTable(transfers) {
  if (!transfers.length) return emptyState("Clock3", "No transfers yet");

  const owned = new Set(state.accounts.map(account => account.id));

  return `
    <div class="table-wrap">
      <table>
        <thead>
          <tr><th>Direction</th><th>Date</th><th>Status</th><th class="number-cell">Amount</th></tr>
        </thead>
        <tbody>
          ${transfers.map(transfer => {
            const incoming = owned.has(transfer.destinationAccountId);
            return `
              <tr>
                <td>
                  <div class="direction-cell ${incoming ? "incoming" : "outgoing"}">
                    <span><i data-lucide="${incoming ? "ArrowDownLeft" : "ArrowUpRight"}"></i></span>
                    <div><strong>${incoming ? "Incoming" : "Outgoing"}</strong><small>${escapeHtml(incoming ? transfer.sourceAccountId : transfer.destinationAccountId)}</small></div>
                  </div>
                </td>
                <td>${formatDate(transfer.createdAt)}</td>
                <td><span class="status status-${transfer.status.toLowerCase()}">${escapeHtml(transfer.status)}</span></td>
                <td class="number-cell ${incoming ? "positive" : ""}">${incoming ? "+" : "−"}${formatMoney(transfer.amount, transfer.currency)}</td>
              </tr>`;
          }).join("")}
        </tbody>
      </table>
    </div>`;
}

function renderPagination() {
  return `
    <div class="pagination">
      <span>Page ${state.transfers.page} of ${Math.max(state.transfers.totalPages, 1)}</span>
      <div>
        <button class="icon-button" id="previous-page" aria-label="Previous page" ${state.transfers.hasPreviousPage ? "" : "disabled"}><i data-lucide="ChevronLeft"></i></button>
        <button class="icon-button" id="next-page" aria-label="Next page" ${state.transfers.hasNextPage ? "" : "disabled"}><i data-lucide="ChevronRight"></i></button>
      </div>
    </div>`;
}

function bindPagination() {
  document.querySelector("#previous-page")?.addEventListener("click", () => loadTransfers(state.transfers.page - 1));
  document.querySelector("#next-page")?.addEventListener("click", () => loadTransfers(state.transfers.page + 1));
}

async function loadTransfers(page) {
  const accountId = document.querySelector("#activity-account")?.value ?? "";
  const status = document.querySelector("#activity-status")?.value ?? "";
  const [sortBy, sortDirection] = (document.querySelector("#activity-sort")?.value ?? "createdAt:desc").split(":");
  const query = new URLSearchParams({ page, pageSize: 25, sortBy, sortDirection });
  if (accountId) query.set("accountId", accountId);
  if (status) query.set("status", status);

  try {
    state.transfers = await api(`/api/transfers?${query}`);
    document.querySelector("#activity-table").innerHTML = renderTransferTable(state.transfers.items);
    document.querySelector(".pagination").outerHTML = renderPagination();
    bindPagination();
    createIcons({ icons });
  } catch (error) {
    showToast(error.message, "error");
  }
}

function emptyState(icon, message) {
  return `<div class="empty-state"><i data-lucide="${icon}"></i><p>${message}</p></div>`;
}

function bindGoButtons() {
  document.querySelectorAll("[data-go]").forEach(button => {
    button.addEventListener("click", () => {
      state.view = button.dataset.go;
      renderView();
    });
  });
}

async function refreshAll(nextView = state.view) {
  const refreshButton = document.querySelector("#refresh-button");
  refreshButton?.classList.add("spinning");

  try {
    await loadData();
    state.view = nextView;
    renderView();
  } catch (error) {
    showToast(error.message, "error");
  } finally {
    refreshButton?.classList.remove("spinning");
  }
}

function setButtonBusy(button, busy) {
  if (!button) return;
  button.disabled = busy;
  button.classList.toggle("busy", busy);
}

function showToast(message, type) {
  const toast = document.createElement("div");
  toast.className = `toast toast-${type}`;
  toast.innerHTML = `<i data-lucide="${type === "success" ? "CheckCircle2" : "CircleDollarSign"}"></i><span>${escapeHtml(message)}</span>`;
  document.querySelector("#toast-region").append(toast);
  createIcons({ icons });
  window.setTimeout(() => toast.remove(), 4500);
}

function renderFatal(error) {
  app.innerHTML = `
    <div class="fatal-screen">
      <div class="brand-mark">SB</div>
      <h1>SecureBank could not start</h1>
      <p>${escapeHtml(error.message)}</p>
      <button class="primary-button" onclick="window.location.reload()">Try again</button>
    </div>`;
}

try {
  const authenticated = await keycloak.init({
    onLoad: "login-required",
    pkceMethod: "S256",
    checkLoginIframe: false
  });

  if (authenticated) {
    await loadData();
    renderShell();
  }
} catch (error) {
  renderFatal(error);
}
