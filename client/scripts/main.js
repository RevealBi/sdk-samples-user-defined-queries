// Reveal Studio - Main JavaScript

// Configuration
const API_BASE_URL = 'http://localhost:5111';
const API_BASE = 'http://localhost:5111/api';

// State
let queries = [];
let currentView = 'queries';

// DOM Elements
const sidebar = document.getElementById('sidebar');
const sidebarToggle = document.getElementById('sidebarToggle');
const createNewBtn = document.getElementById('createNewBtn');
const createQueryBtn = document.getElementById('createQueryBtn');
const queriesGrid = document.getElementById('queriesGrid');
const emptyState = document.getElementById('emptyState');
const searchInput = document.getElementById('searchInput');
const loadingIndicator = document.getElementById('loadingIndicator');
const queriesPage = document.getElementById('queriesPage');
const dashboardPage = document.getElementById('dashboardPage');
const dashboardFrame = document.getElementById('dashboardFrame');
const dashboardTitle = document.getElementById('dashboardTitle');
const dashboardsPage = document.getElementById('dashboardsPage');
const dashboardSelect = document.getElementById('dashboardSelect');
const dashboardLoadingStatus = document.getElementById('dashboardLoadingStatus');
const revealViewContainer = document.getElementById('revealViewContainer');

// Modal elements
const createQueryModal = document.getElementById('createQueryModal');
const queryNameInput = document.getElementById('queryName');
const queryDescriptionInput = document.getElementById('queryDescription');
const tablesList = document.getElementById('tablesList');
const fieldsContainer = document.getElementById('fieldsContainer');

// Query builder state
let allowedTables = [];
let currentTableSchema = null;

// Dashboards page state
let revealView = null;
let dashboardsList = [];

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    initApp();
});

function initApp() {
    setupEventListeners();
    loadQueries();
    loadAllowedTables();
}

// Event Listeners
function setupEventListeners() {
    // Sidebar toggle
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', toggleSidebar);
    }

    // Create new buttons
    if (createNewBtn) {
        createNewBtn.addEventListener('click', showQueryBuilder);
    }
    if (createQueryBtn) {
        createQueryBtn.addEventListener('click', showQueryBuilder);
    }

    // Search functionality
    if (searchInput) {
        searchInput.addEventListener('input', handleSearch);
    }

    // Navigation links
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const page = link.getAttribute('data-page');
            navigateToPage(page);
            
            // Update active state
            navLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');
        });
    });
    
    // Close modal when clicking outside
    if (createQueryModal) {
        createQueryModal.addEventListener('click', (e) => {
            if (e.target === createQueryModal) {
                closeCreateQueryModal();
            }
        });
    }
}

// Sidebar toggle
function toggleSidebar() {
    if (sidebar) {
        sidebar.classList.toggle('collapsed');
    }
}

// Navigation
function navigateToPage(page) {
    currentView = page;
    
    if (page === 'queries') {
        showQueriesPage();
    } else if (page === 'dashboards') {
        showDashboardsPage();
    }
}

function showQueriesPage() {
    if (queriesPage) {
        queriesPage.classList.remove('hidden');
        queriesPage.style.display = 'block';
    }
    if (dashboardPage) {
        dashboardPage.classList.add('hidden');
        dashboardPage.style.display = 'none';
    }
    if (dashboardsPage) {
        dashboardsPage.classList.add('hidden');
        dashboardsPage.style.display = 'none';
    }
    currentView = 'queries';
    loadQueries();
}

function showDashboardsPage() {
    if (queriesPage) {
        queriesPage.classList.add('hidden');
        queriesPage.style.display = 'none';
    }
    if (dashboardPage) {
        dashboardPage.classList.add('hidden');
        dashboardPage.style.display = 'none';
    }
    if (dashboardsPage) {
        dashboardsPage.classList.remove('hidden');
        dashboardsPage.style.display = 'block';
    }
    currentView = 'dashboards';
    
    // Initialize Reveal SDK if needed
    initializeRevealSdk();
    
    // Load dashboards list
    loadDashboardNames();
}

function showQueryBuilder() {
    // Open query builder modal instead of navigating
    openCreateQueryModal();
}

// Modal functions
function openCreateQueryModal() {
    if (!createQueryModal) return;
    
    createQueryModal.classList.add('show');
    
    // Reset form
    if (queryNameInput) queryNameInput.value = '';
    if (queryDescriptionInput) queryDescriptionInput.value = '';
    
    // Reset selections
    document.querySelectorAll('.table-item').forEach(item => {
        item.classList.remove('selected');
    });
    
    if (fieldsContainer) {
        fieldsContainer.innerHTML = '<div class="fields-empty">Select a table to view its fields</div>';
    }
    
    currentTableSchema = null;
}

function closeCreateQueryModal() {
    if (!createQueryModal) return;
    createQueryModal.classList.remove('show');
}

// Load allowed tables from the server
async function loadAllowedTables() {
    try {
        const response = await fetch(`${API_BASE}/allowed-tables`);
        if (!response.ok) throw new Error('Failed to load allowed tables');
        
        allowedTables = await response.json();
        populateTablesList();
    } catch (error) {
        console.error('Error loading allowed tables:', error);
    }
}

// Populate the tables list in the left panel
function populateTablesList() {
    if (!tablesList) return;
    
    if (allowedTables.length === 0) {
        tablesList.innerHTML = '<div class="fields-empty">No tables available</div>';
        return;
    }

    let tablesHTML = '';
    allowedTables.forEach(table => {
        tablesHTML += `
            <div class="table-item" onclick="selectTable('${escapeHtml(table.name)}')" data-table="${escapeHtml(table.name)}">
                <div class="table-item-name">${escapeHtml(table.displayName)}</div>
                <div class="table-item-description">${escapeHtml(table.description)}</div>
            </div>
        `;
    });

    tablesList.innerHTML = tablesHTML;
}

// Select a table and load its fields
async function selectTable(tableName) {
    // Update UI to show selected table
    document.querySelectorAll('.table-item').forEach(item => {
        item.classList.remove('selected');
    });
    const selectedItem = document.querySelector(`[data-table="${tableName}"]`);
    if (selectedItem) {
        selectedItem.classList.add('selected');
    }

    // Load table fields
    await loadTableFields(tableName);
}

// Load table fields when a table is selected
async function loadTableFields(tableName) {
    if (!fieldsContainer) return;
    
    try {
        fieldsContainer.innerHTML = '<div class="fields-empty">Loading fields...</div>';

        const response = await fetch(`${API_BASE}/table-schema/${tableName}`);
        if (!response.ok) throw new Error('Failed to load table schema');
        
        currentTableSchema = await response.json();
        populateFieldsCheckboxes();
    } catch (error) {
        console.error('Error loading table schema:', error);
        fieldsContainer.innerHTML = '<div class="fields-empty" style="color: #dc3545;">Failed to load table fields. Please check your database connection.</div>';
    }
}

// Populate fields checkboxes with select all/deselect all controls
function populateFieldsCheckboxes() {
    if (!fieldsContainer || !currentTableSchema || !currentTableSchema.columns) {
        if (fieldsContainer) {
            fieldsContainer.innerHTML = '<div class="fields-empty">No fields found</div>';
        }
        return;
    }

    let fieldsHTML = `
        <div class="fields-controls">
            <div>
                <strong>${currentTableSchema.columns.length} fields available</strong>
            </div>
            <div class="fields-controls-buttons">
                <button type="button" class="control-btn" onclick="selectAllFields()">Select All</button>
                <button type="button" class="control-btn" onclick="deselectAllFields()">Deselect All</button>
            </div>
        </div>
    `;

    currentTableSchema.columns.forEach(column => {
        const maxLengthStr = column.maxLength ? `(${column.maxLength})` : '';
        const nullableStr = column.isNullable ? ', nullable' : ', not null';
        
        fieldsHTML += `
            <div class="field-checkbox">
                <input type="checkbox" 
                       id="field_${escapeHtml(column.columnName)}" 
                       value="${escapeHtml(column.columnName)}"
                       onchange="updateFieldSelection()">
                <label for="field_${escapeHtml(column.columnName)}">
                    ${escapeHtml(column.columnName)}
                    <span class="field-info">(${escapeHtml(column.dataType)}${maxLengthStr}${nullableStr})</span>
                </label>
            </div>
        `;
    });

    fieldsContainer.innerHTML = fieldsHTML;
}

// Select all fields
function selectAllFields() {
    document.querySelectorAll('#fieldsContainer input[type="checkbox"]').forEach(checkbox => {
        checkbox.checked = true;
    });
    updateFieldSelection();
}

// Deselect all fields
function deselectAllFields() {
    document.querySelectorAll('#fieldsContainer input[type="checkbox"]').forEach(checkbox => {
        checkbox.checked = false;
    });
    updateFieldSelection();
}

// Update field selection count
function updateFieldSelection() {
    const selectedCount = document.querySelectorAll('#fieldsContainer input[type="checkbox"]:checked').length;
    console.log(`Selected ${selectedCount} fields`);
}

// Apply query - generate and save the SQL with metadata
async function applyQuery() {
    const queryName = queryNameInput ? queryNameInput.value.trim() : '';
    const queryDescription = queryDescriptionInput ? queryDescriptionInput.value.trim() : '';
    const selectedTable = document.querySelector('.table-item.selected');
    const selectedFields = getSelectedFields();
    
    if (!queryName) {
        alert('Please enter a friendly name for the query');
        return;
    }
    
    if (!selectedTable) {
        alert('Please select a table');
        return;
    }
    
    if (selectedFields.length === 0) {
        alert('Please select at least one field');
        return;
    }

    showLoading(true);
    
    try {
        const queryId = generateGUID();
        const tableName = selectedTable.getAttribute('data-table');
        
        const requestData = {
            id: queryId,
            friendlyName: queryName,
            description: queryDescription,
            tableName: tableName,
            fields: selectedFields
        };

        const response = await fetch(`${API_BASE}/generate-query`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) throw new Error('Failed to generate query');
        
        const result = await response.json();
        console.log('Query generated:', result);
        
        // Close modal and refresh queries
        closeCreateQueryModal();
        await loadQueries();
        
        //alert(`Query "${queryName}" created successfully!`);
    } catch (error) {
        console.error('Error generating query:', error);
        alert('Failed to generate query: ' + error.message);
    } finally {
        showLoading(false);
    }
}

// Get selected fields from checkboxes
function getSelectedFields() {
    const checkboxes = document.querySelectorAll('#fieldsContainer input[type="checkbox"]:checked');
    return Array.from(checkboxes).map(cb => cb.value);
}

// Generate a GUID
function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function showDashboardViewer(queryGuid, queryTitle, mode = 'view', dashboardId = null) {
    if (!queriesPage || !dashboardPage || !dashboardFrame || !dashboardTitle) return;
    
    queriesPage.classList.add('hidden');
    queriesPage.style.display = 'none';
    dashboardPage.classList.remove('hidden');
    dashboardPage.style.display = 'block';
    
    // Update title
    dashboardTitle.textContent = mode === 'edit' ? `Edit Dashboard - ${queryTitle}` : `Dashboard - ${queryTitle}`;
    
    // Load appropriate page based on mode
    let url;
    if (mode === 'edit') {
        url = `create-dashboard.html?guid=${queryGuid}`;
    } else {
        // For view mode, use the dashboard ID if provided
        const viewDashboardId = dashboardId || `user_${queryGuid}`;
        url = `create-data-grid.html?dashboardId=${viewDashboardId}`;
    }
    
    dashboardFrame.src = url;
    currentView = 'dashboard';
}

// Search functionality
function handleSearch(e) {
    const searchTerm = e.target.value.toLowerCase();
    filterQueries(searchTerm);
}

function filterQueries(searchTerm) {
    const filteredQueries = queries.filter(query => {
        return query.name.toLowerCase().includes(searchTerm) ||
               query.description.toLowerCase().includes(searchTerm) ||
               query.guid.toLowerCase().includes(searchTerm);
    });
    
    renderQueries(filteredQueries);
}

// Load queries from API
async function loadQueries() {
    showLoading(true);
    
    try {
        const response = await fetch(`${API_BASE}/queries`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const queriesData = await response.json();
        
        // Transform the API response to match our card format
        queries = queriesData.map(q => ({
            guid: q.fileName.replace('.json', '').replace('.txt', ''),
            name: q.friendlyName,
            description: q.description || 'No description provided',
            sql: q.sql || `SELECT * FROM ${q.tableName}`,
            tableName: q.tableName,
            fieldCount: q.fieldCount,
            createdDate: q.dateAdded,
            fileName: q.fileName
        }));
        
        renderQueries(queries);
    } catch (error) {
        console.error('Error loading queries:', error);
        showError('Failed to load queries. Please ensure the server is running.');
        queries = [];
        renderQueries([]);
    } finally {
        showLoading(false);
    }
}

// Render queries grid
function renderQueries(queriesToRender) {
    if (!queriesGrid || !emptyState) return;
    
    // Show empty state if no queries
    if (queriesToRender.length === 0) {
        queriesGrid.style.display = 'none';
        emptyState.classList.remove('hidden');
        emptyState.style.display = 'block';
        return;
    }
    
    queriesGrid.style.display = 'grid';
    emptyState.classList.add('hidden');
    emptyState.style.display = 'none';
    
    // Clear grid
    queriesGrid.innerHTML = '';
    
    // Render each query card
    queriesToRender.forEach(query => {
        const card = createQueryCard(query);
        queriesGrid.appendChild(card);
    });
}

// Create query card element
function createQueryCard(query) {
    const card = document.createElement('div');
    card.className = 'query-card';
    
    // Truncate SQL for display - if we have it from the backend, otherwise create a simple one
    const sqlText = query.sql || `SELECT ${query.fieldCount || '*'} fields FROM ${query.tableName}`;
    const sqlPreview = sqlText.substring(0, 150) + (sqlText.length > 150 ? '...' : '');
    
    card.innerHTML = `
        <div class="query-card-header">
            <div>
                <div class="query-card-title">${escapeHtml(query.name || 'Untitled Query')}</div>
                <div class="query-card-guid">${escapeHtml(query.guid)}</div>
            </div>
            <button class="query-card-menu" onclick="showQueryMenu('${query.guid}')" title="Delete query">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="1"/><circle cx="12" cy="5" r="1"/><circle cx="12" cy="19" r="1"/>
                </svg>
            </button>
        </div>
        <div class="query-card-body">
            <p class="query-card-description">${escapeHtml(query.description || 'No description provided')}</p>
            <div class="query-card-meta" style="margin-top: 0.5rem; margin-bottom: 0.5rem; font-size: 0.75rem; color: #6b7280;">
                <strong>Table:</strong> ${escapeHtml(query.tableName || 'N/A')} | 
                <strong>Fields:</strong> ${query.fieldCount || 0}
            </div>
            <div class="query-card-sql">${escapeHtml(sqlPreview)}</div>
        </div>
        <div class="query-card-footer">
            <div class="query-card-meta">
                Created: ${formatDate(query.createdDate)}
            </div>
            <div class="query-card-actions">
                <button class="btn btn-secondary btn-sm" onclick="showGrid('${query.guid}', '${escapeHtml(query.name)}')">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect width="7" height="7" x="3" y="3" rx="1"/><rect width="7" height="7" x="14" y="3" rx="1"/><rect width="7" height="7" x="14" y="14" rx="1"/><rect width="7" height="7" x="3" y="14" rx="1"/>
                    </svg>
                    Show Grid
                </button>
                <button class="btn btn-primary btn-sm" onclick="editDashboard('${query.guid}', '${escapeHtml(query.name)}')">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M12 20h9"/><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z"/>
                    </svg>
                    Edit Dashboard
                </button>
            </div>
        </div>
    `;
    
    return card;
}

// Query actions
function showGrid(guid, name) {
    showLoading(true);
    
    // Call the API to generate the grid dashboard
    fetch(`${API_BASE}/generate-grid-dashboard/${guid}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Failed to generate grid dashboard');
        }
        return response.json();
    })
    .then(result => {
        console.log('Grid dashboard generated:', result);
        const dashboardId = result.dashboardId || `user_${guid}`;
        
        // Show in dashboard viewer
        showDashboardViewer(guid, name, 'view', dashboardId);
    })
    .catch(error => {
        console.error('Error generating grid dashboard:', error);
        showError(`Failed to generate grid dashboard: ${error.message}`);
    })
    .finally(() => {
        showLoading(false);
    });
}

function editDashboard(guid, name) {
    // Validate GUID format
    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    
    if (!guidRegex.test(guid)) {
        showError('Invalid query format. Expected GUID-based identifier.');
        return;
    }
    
    showDashboardViewer(guid, name, 'edit');
}

function showQueryMenu(guid) {
    const query = queries.find(q => q.guid === guid);
    if (!query) return;
    
    const confirmDelete = confirm(`Delete query "${query.name}"?\n\nThis will delete:\n- The query file (${guid}.json)\n- The associated dashboard file (user_${guid}.rdash)\n\nThis action cannot be undone.`);
    
    if (confirmDelete) {
        deleteQuery(guid, query.name);
    }
}

async function deleteQuery(guid, name) {
    showLoading(true);
    
    try {
        const response = await fetch(`${API_BASE}/queries/${guid}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(error || 'Failed to delete query');
        }

        const result = await response.json();
        console.log('Query deleted:', result);
        
        // Show success and reload
        alert(`Successfully deleted "${name}"`);
        await loadQueries();
    } catch (error) {
        console.error('Error deleting query:', error);
        showError(`Failed to delete query: ${error.message}`);
    } finally {
        showLoading(false);
    }
}

// Utility functions
function showLoading(show) {
    if (loadingIndicator) {
        if (show) {
            loadingIndicator.classList.remove('hidden');
        } else {
            loadingIndicator.classList.add('hidden');
        }
    }
}

function showError(message) {
    // TODO: Implement toast/notification system
    alert(message);
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return String(text).replace(/[&<>"']/g, m => map[m]);
}

function formatDate(dateString) {
    if (!dateString) return 'Unknown';
    
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} min${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    
    return date.toLocaleDateString();
}

// Make functions available globally
window.showQueriesPage = showQueriesPage;
window.showQueryBuilder = showQueryBuilder;
window.showGrid = showGrid;
window.editDashboard = editDashboard;
window.showQueryMenu = showQueryMenu;
window.openCreateQueryModal = openCreateQueryModal;
window.closeCreateQueryModal = closeCreateQueryModal;
window.selectTable = selectTable;
window.selectAllFields = selectAllFields;
window.deselectAllFields = deselectAllFields;
window.updateFieldSelection = updateFieldSelection;
window.applyQuery = applyQuery;

// ========================================
// DASHBOARDS PAGE FUNCTIONALITY
// ========================================

// Initialize Reveal SDK
function initializeRevealSdk() {
    if (typeof $ !== 'undefined' && $.ig && $.ig.RevealSdkSettings) {
        $.ig.RevealSdkSettings.setBaseUrl(API_BASE_URL + '/');
    }
}

// Load dashboard names from API
async function loadDashboardNames() {
    if (!dashboardSelect) return;
    
    try {
        dashboardSelect.innerHTML = '<option value="">Loading dashboards...</option>';
        
        const response = await fetch(`${API_BASE_URL}/dashboards/names`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        dashboardsList = await response.json();
        populateDashboardDropdown();
        
        // Load first dashboard by default
        if (dashboardsList.length > 0) {
            loadDashboardById(dashboardsList[0].dashboardFileName);
        } else {
            dashboardSelect.innerHTML = '<option value="">No dashboards available</option>';
        }
    } catch (error) {
        console.error('Error loading dashboard names:', error);
        dashboardSelect.innerHTML = '<option value="">Error loading dashboards</option>';
    }
}

// Populate the dropdown with dashboard names
function populateDashboardDropdown() {
    if (!dashboardSelect) return;
    
    dashboardSelect.innerHTML = '';
    
    dashboardsList.forEach((dashboard, index) => {
        const option = document.createElement('option');
        option.value = dashboard.dashboardFileName;
        option.textContent = dashboard.dashboardTitle;
        if (index === 0) {
            option.selected = true;
        }
        dashboardSelect.appendChild(option);
    });
    
    // Add change event listener
    dashboardSelect.addEventListener('change', function() {
        if (this.value) {
            loadDashboardById(this.value);
        }
    });
}

// Load dashboard by ID
async function loadDashboardById(dashboardFileName) {
    if (!revealViewContainer) return;
    
    // Show loading indicator
    if (dashboardLoadingStatus) {
        dashboardLoadingStatus.classList.add('show');
    }
    
    try {
        // Wait for jQuery and Reveal SDK to be available
        if (typeof $ === 'undefined' || !$.ig || !$.ig.RVDashboard) {
            console.error('Reveal SDK not loaded');
            return;
        }
        
        const dashboard = await $.ig.RVDashboard.loadDashboard(dashboardFileName);
        
        if (!revealView) {
            revealView = new $.ig.RevealView('#revealViewContainer');
            revealView.interactiveFilteringEnabled = true;
        }
        
        revealView.dashboard = dashboard;
        
        // Hide loading indicator
        if (dashboardLoadingStatus) {
            dashboardLoadingStatus.classList.remove('show');
        }
    } catch (error) {
        console.error('Error loading dashboard:', error);
        
        // Hide loading indicator
        if (dashboardLoadingStatus) {
            dashboardLoadingStatus.classList.remove('show');
        }
        
        alert(`Failed to load dashboard: ${error.message}`);
    }
}
