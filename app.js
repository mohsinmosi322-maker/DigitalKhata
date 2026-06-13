var currentTable = null;
var allCustomers = [];
var currentUserRole = localStorage.getItem('dk_role') || 'Operator';
var currentUserName = localStorage.getItem('dk_user') || 'User';

window.addEventListener('DOMContentLoaded', function() {
    if (currentUserRole === 'Admin') {
        var adminMenu = document.getElementById('adminMenuItem');
        if (adminMenu) adminMenu.style.display = 'block';
    }
    var userNameEl = document.getElementById('userName');
    if (userNameEl) userNameEl.innerText = currentUserName + ' (' + currentUserRole + ')';
});

document.addEventListener('keydown', function(e) {
    if (e.key === 'Enter' && (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT' || (e.target.tagName === 'TEXTAREA' && !e.shiftKey))) {
        e.preventDefault();
        const form = e.target.closest('form');
        if (form) {
            const focusableElements = form.querySelectorAll('input, select, textarea');
            const currentIndex = Array.prototype.indexOf.call(focusableElements, e.target);
            
            if (currentIndex > -1 && currentIndex < focusableElements.length - 1) {
                focusableElements[currentIndex + 1].focus();
            } else {
                const modal = form.closest('.modal');
                if (modal) {
                    const saveBtn = modal.querySelector('.btn-primary');
                    if (saveBtn && !saveBtn.disabled) {
                        saveBtn.click();
                    }
                }
            }
        }
    }
});

function loadPage(page, el) {
    if (el) {
        document.querySelectorAll('.nav-link').forEach(function(e) { e.classList.remove('active'); });
        el.classList.add('active');
    }
    var c = document.getElementById('content');
    if (page === 'dashboard') { loadDashboard(c); }
    else if (page === 'customers') { loadCustomers(c); }
    else if (page === 'debits') { loadDebits(c); }
    else if (page === 'recoveries') { loadRecoveries(c); }
    else if (page === 'closed') { loadClosed(c); }
    else if (page === 'admin') { loadAdmin(c); }
}

function loadDashboard(c) {
    c.innerHTML = `<div class="container-fluid"> 
        <h3 class="page-title"><i class="fa-solid fa-chart-line"></i> Dashboard Overview</h3> 
        <div class="row g-4"> 
            <div class="col-md-3"> 
                <div class="card stat-card p-3 position-relative"> 
                    <i class="fa-solid fa-users stat-icon"></i> 
                    <h5><i class="fa-solid fa-users"></i> Active Khatas</h5> 
                    <h2 id="statCustomers">...</h2> 
                </div> 
            </div> 
            <div class="col-md-3"> 
                <div class="card stat-card p-3 position-relative" style="border-left-color: #dc3545;"> 
                    <i class="fa-solid fa-arrow-trend-up stat-icon" style="color: #dc3545;"></i> 
                    <h5><i class="fa-solid fa-arrow-trend-up"></i> Total Debit (Udhaar)</h5> 
                    <h2 id="statDebit" style="color: #dc3545;">...</h2> 
                </div> 
            </div> 
            <div class="col-md-3"> 
                <div class="card stat-card p-3 position-relative" style="border-left-color: #28a745;"> 
                    <i class="fa-solid fa-arrow-trend-down stat-icon" style="color: #28a745;"></i> 
                    <h5><i class="fa-solid fa-arrow-trend-down"></i> Total Recovery</h5> 
                    <h2 id="statRecovery" style="color: #28a745;">...</h2> 
                </div> 
            </div> 
            <div class="col-md-3"> 
                <div class="card stat-card p-3 position-relative" style="border-left-color: #ffc107;"> 
                    <i class="fa-solid fa-coins stat-icon" style="color: #ffc107;"></i> 
                    <h5><i class="fa-solid fa-coins"></i> Outstanding Balance</h5> 
                    <h2 id="statOutstanding" style="color: #e0a800;">...</h2> 
                </div> 
            </div> 
        </div> 
    </div>`;
    fetch('/api/dashboard').then(r => r.json()).then(d => {
        if (d.error) { c.innerHTML = '<div class="alert alert-danger">Error: ' + d.error + '</div>'; return; }
        document.getElementById('statCustomers').innerText = d.customers;
        document.getElementById('statDebit').innerText = 'Rs. ' + parseFloat(d.totalDebit).toFixed(2);
        document.getElementById('statRecovery').innerText = 'Rs. ' + parseFloat(d.totalRecovery).toFixed(2);
        document.getElementById('statOutstanding').innerText = 'Rs. ' + parseFloat(d.outstanding).toFixed(2);
    }).catch(err => console.error('Dashboard error:', err));
}

function loadCustomers(c) {
    c.innerHTML = `<div class="container-fluid"> 
        <div class="d-flex justify-content-between align-items-center mb-4"> 
            <h3 class="page-title mb-0"><i class="fa-solid fa-users"></i> Active Khatas</h3> 
            <button class="btn btn-primary" onclick="showAddCustomerModal()"><i class="fa-solid fa-plus me-1"></i> Add New Customer</button> 
        </div> 
        <div class="card"> 
            <div class="card-header"><i class="fa-solid fa-list"></i> Active Customer Accounts</div> 
            <div class="card-body"> 
                <table id="customersTable" class="table table-striped table-bordered" style="width:100%"> 
                    <thead><tr><th>Code</th><th>Name</th><th>Mobile</th><th>City</th><th>Balance</th><th>Actions</th></tr></thead> 
                    <tbody></tbody> 
                </table> 
            </div> 
        </div> 
    </div>`;
    fetch('/api/customers').then(r => r.json()).then(data => {
        if (data.error) { alert('Error: ' + data.error); return; }
        allCustomers = data;
        var tb = $('#customersTable tbody'); tb.empty();
        data.forEach(x => {
            var cls = x.CurrentBalance > 0 ? 'text-danger fw-bold' : 'text-success fw-bold';
            var row = `<tr>
                <td><strong>${x.CustomerCode}</strong></td>
                <td>${x.CustomerName}</td>
                <td>${x.Mobile || '-'}</td>
                <td>${x.City || '-'}</td>
                <td class="${cls}">Rs. ${parseFloat(x.CurrentBalance).toFixed(2)}</td>
                <td>
                    <button class="btn btn-sm btn-info me-1" onclick="showLedger(${x.CustomerID})" title="View Ledger"><i class="fa-solid fa-book-open"></i></button>
                    <button class="btn btn-sm btn-danger" onclick="showCloseKhataModal(${x.CustomerID}, '${x.CustomerName.replace(/'/g, "\\'")}')" title="Close Khata"><i class="fa-solid fa-xmark"></i> Close</button>
                </td>
            </tr>`;
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); }
        currentTable = $('#customersTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(err => alert('Error loading customers: ' + err));
}

function showAddCustomerModal() {
    var modalHtml = `<div class="modal fade" id="customerModal" tabindex="-1"> 
        <div class="modal-dialog"> 
            <div class="modal-content"> 
                <div class="modal-header"> 
                    <h5 class="modal-title"><i class="fa-solid fa-user-plus me-2"></i>Add New Customer</h5> 
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button> 
                </div> 
                <div class="modal-body"> 
                    <form id="customerForm"> 
                        <div class="row"> 
                            <div class="col-md-6 mb-3"><label class="form-label">Customer Code *</label><input type="text" class="form-control" id="custCode" required></div> 
                            <div class="col-md-6 mb-3"><label class="form-label">Customer Name *</label><input type="text" class="form-control" id="custName" required></div> 
                        </div> 
                        <div class="row"> 
                            <div class="col-md-6 mb-3"><label class="form-label">Mobile</label><input type="text" class="form-control" id="custMobile" placeholder="03XX-XXXXXXX"></div> 
                            <div class="col-md-6 mb-3"><label class="form-label">City</label><input type="text" class="form-control" id="custCity"></div> 
                        </div> 
                        <div class="mb-3"><label class="form-label">Opening Balance (Rs.)</label><input type="number" step="0.01" class="form-control" id="custOB" value="0"></div> 
                    </form> 
                </div> 
                <div class="modal-footer"> 
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal"><i class="fa-solid fa-xmark me-1"></i>Cancel</button> 
                    <button type="button" class="btn btn-primary" onclick="submitCustomer()"><i class="fa-solid fa-check me-1"></i>Save Customer</button> 
                </div> 
            </div> 
        </div> 
    </div>`;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    var modal = new bootstrap.Modal(document.getElementById('customerModal'));
    modal.show();
    setTimeout(() => document.getElementById('custCode').focus(), 300);
    document.getElementById('customerModal').addEventListener('hidden.bs.modal', function () { this.remove(); });
}

function submitCustomer() {
    var code = document.getElementById('custCode').value;
    var name = document.getElementById('custName').value;
    var mob = document.getElementById('custMobile').value;
    var city = document.getElementById('custCity').value;
    var ob = document.getElementById('custOB').value || 0;
    if(!code || !name) { alert('Code and Name are required!'); return; }
    fetch('/api/customers', {
        method: 'POST', headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerCode: code, CustomerName: name, Mobile: mob, City: city, OpeningBalance: ob })
    }).then(r => r.json()).then(res => {
        bootstrap.Modal.getInstance(document.getElementById('customerModal')).hide();
        if(res.success) { alert('Customer added successfully!'); loadPage('customers', document.querySelectorAll('.nav-link')[1]); }
        else { alert('Error: ' + res.message); }
    }).catch(err => alert('Error: ' + err));
}

function showCloseKhataModal(id, name) {
    var modalHtml = `<div class="modal fade" id="closeModal" tabindex="-1"> 
        <div class="modal-dialog"> 
            <div class="modal-content"> 
                <div class="modal-header bg-danger text-white"> 
                    <h5 class="modal-title"><i class="fa-solid fa-triangle-exclamation me-2"></i>Close Khata Confirmation</h5> 
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button> 
                </div> 
                <div class="modal-body"> 
                    <p>Are you sure you want to close the khata for <strong>${name}</strong>?</p> 
                    <div class="alert alert-warning small mb-0"><i class="fa-solid fa-circle-info me-1"></i> Once closed, this customer will move to "Closed Khatas" and will not appear in Active, Debit, or Recovery lists.</div> 
                </div> 
                <div class="modal-footer"> 
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button> 
                    <button type="button" class="btn btn-danger" onclick="confirmCloseKhata(${id})"><i class="fa-solid fa-xmark me-1"></i>Close Khata</button> 
                </div> 
            </div> 
        </div> 
    </div>`;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    var modal = new bootstrap.Modal(document.getElementById('closeModal'));
    modal.show();
    document.getElementById('closeModal').addEventListener('hidden.bs.modal', function () { this.remove(); });
}

function confirmCloseKhata(id) {
    fetch('/api/customers/close', {
        method: 'POST', headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerID: id })
    }).then(r => r.json()).then(res => {
        bootstrap.Modal.getInstance(document.getElementById('closeModal')).hide();
        if(res.success) { alert('Khata closed successfully!'); loadPage('customers', document.querySelectorAll('.nav-link')[1]); }
        else { alert('Error: ' + res.message); }
    }).catch(err => alert('Error: ' + err));
}

function loadDebits(c) {
    c.innerHTML = `<div class="container-fluid"> 
        <div class="d-flex justify-content-between align-items-center mb-4"> 
            <h3 class="page-title mb-0"><i class="fa-solid fa-file-invoice-dollar"></i> Debit Entries (Udhaar)</h3> 
            <button class="btn btn-primary" onclick="showAddDebitModal()"><i class="fa-solid fa-plus me-1"></i> New Debit Entry</button> 
        </div> 
        <div class="card"> 
            <div class="card-header"><i class="fa-solid fa-list"></i> Recent Debit Transactions</div> 
            <div class="card-body"> 
                <table id="debitsTable" class="table table-striped table-bordered" style="width:100%"> 
                    <thead><tr><th>Date</th><th>Customer</th><th>Invoice #</th><th>Amount</th><th>Description</th></tr></thead> 
                    <tbody></tbody> 
                </table> 
            </div> 
        </div> 
    </div>`;
    fetch('/api/debits').then(r => r.json()).then(data => {
        if (data.error) { alert('Error: ' + data.error); return; }
        var tb = $('#debitsTable tbody'); tb.empty();
        data.forEach(x => {
            var row = `<tr><td>${x.DebitDate}</td><td>${x.CustomerName}</td><td>${x.InvoiceNumber || '-'}</td><td class="text-danger fw-bold">Rs. ${parseFloat(x.Amount).toFixed(2)}</td><td>${x.Description || '-'}</td></tr>`;
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); }
        currentTable = $('#debitsTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(err => alert('Error loading debits: ' + err));
}

function showAddDebitModal() {
    if (allCustomers.length === 0) { alert('No active customers! Add customer first.'); return; }
    var selectHtml = '<option value="">-- Select Customer --</option>';
    allCustomers.forEach(x => { selectHtml += `<option value="${x.CustomerID}">${x.CustomerName} (Bal: Rs.${parseFloat(x.CurrentBalance).toFixed(2)})</option>`; });
    var modalHtml = `
        <div class="modal fade" id="debitModal" tabindex="-1">
            <div class="modal-dialog"> <div class="modal-content">
                <div class="modal-header"> <h5 class="modal-title"> <i class="fa-solid fa-file-invoice-dollar me-2"></i>New Debit Entry </h5> <button type="button" class="btn-close" data-bs-dismiss="modal"></button> </div>
                <div class="modal-body"> <form id="debitForm">
                    <div class="mb-3"> <label class="form-label">Date </label> <input type="date" class="form-control" id="debitDate" required> </div>
                    <div class="mb-3"> <label class="form-label">Customer * </label> <select class="form-select" id="debitCustomer" required>${selectHtml}</select> </div>
                    <div class="mb-3"> <label class="form-label">Invoice Number </label> <input type="text" class="form-control" id="debitInvoice"> </div>
                    <div class="mb-3"> <label class="form-label">Amount (Rs.) * </label> <input type="number" step="0.01" class="form-control" id="debitAmount" required> </div>
                    <div class="mb-3"> <label class="form-label">Description </label> <textarea class="form-control" id="debitDesc" rows="2"></textarea> </div>
                </form> </div>
                <div class="modal-footer"> <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button> <button type="button" class="btn btn-primary" onclick="submitDebit()"> <i class="fa-solid fa-check me-1"></i>Save</button> </div>
            </div> </div>
        </div>`;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    document.getElementById('debitDate').valueAsDate = new Date();
    var modal = new bootstrap.Modal(document.getElementById('debitModal'));
    modal.show();
    setTimeout(() => document.getElementById('debitCustomer').focus(), 300);
    document.getElementById('debitModal').addEventListener('hidden.bs.modal', function () { this.remove(); });
}

function submitDebit() {
    var cid = document.getElementById('debitCustomer').value;
    var date = document.getElementById('debitDate').value;
    var inv = document.getElementById('debitInvoice').value;
    var amt = document.getElementById('debitAmount').value;
    var desc = document.getElementById('debitDesc').value;
    if (!cid || !amt) { alert('Customer and Amount are required!'); return; }
    fetch('/api/debits', {
        method: 'POST', headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerID: cid, DebitDate: date, InvoiceNumber: inv, Amount: amt, Description: desc })
    }).then(r => r.json()).then(res => {
        bootstrap.Modal.getInstance(document.getElementById('debitModal')).hide();
        if(res.success) { alert('Debit added successfully!'); loadPage('debits', document.querySelectorAll('.nav-link')[2]); }
        else { alert('Error: ' + res.message); }
    }).catch(err => alert('Error: ' + err));
}

function loadRecoveries(c) {
    c.innerHTML = `<div class="container-fluid"> 
        <div class="d-flex justify-content-between align-items-center mb-4"> 
            <h3 class="page-title mb-0"><i class="fa-solid fa-hand-holding-dollar"></i> Recovery Entries (Vasooli)</h3> 
            <button class="btn btn-primary" onclick="showAddRecoveryModal()"><i class="fa-solid fa-plus me-1"></i> New Recovery Entry</button> 
        </div> 
        <div class="card"> 
            <div class="card-header"><i class="fa-solid fa-list"></i> Recent Recovery Transactions</div> 
            <div class="card-body"> 
                <table id="recoveriesTable" class="table table-striped table-bordered" style="width:100%"> 
                    <thead><tr><th>Date</th><th>Customer</th><th>Amount</th><th>Method</th><th>Reference</th></tr></thead> 
                    <tbody></tbody> 
                </table> 
            </div> 
        </div> 
    </div>`;
    fetch('/api/recoveries').then(r => r.json()).then(data => {
        if (data.error) { alert('Error: ' + data.error); return; }
        var tb = $('#recoveriesTable tbody'); tb.empty();
        data.forEach(x => {
            var row = `<tr><td>${x.RecoveryDate}</td><td>${x.CustomerName}</td><td class="text-success fw-bold">Rs. ${parseFloat(x.AmountReceived).toFixed(2)}</td><td>${x.PaymentMethod}</td><td>${x.ReferenceNumber || '-'}</td></tr>`;
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); }
        currentTable = $('#recoveriesTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(err => alert('Error loading recoveries: ' + err));
}

function showAddRecoveryModal() {
    if (allCustomers.length === 0) { alert('No active customers!'); return; }
    var selectHtml = '<option value="">-- Select Customer --</option>';
    allCustomers.forEach(x => { selectHtml += `<option value="${x.CustomerID}">${x.CustomerName}</option>`; });
    var modalHtml = `
        <div class="modal fade" id="recoveryModal" tabindex="-1">
            <div class="modal-dialog"> <div class="modal-content">
                <div class="modal-header"> <h5 class="modal-title"> <i class="fa-solid fa-hand-holding-dollar me-2"></i>New Recovery Entry </h5> <button type="button" class="btn-close" data-bs-dismiss="modal"></button> </div>
                <div class="modal-body"> <form id="recoveryForm">
                    <div class="mb-3"> <label class="form-label">Date </label> <input type="date" class="form-control" id="recoveryDate" required> </div>
                    <div class="mb-3"> <label class="form-label">Customer * </label> <select class="form-select" id="recoveryCustomer" required>${selectHtml}</select> </div>
                    <div class="mb-3"> <label class="form-label">Amount Received (Rs.) * </label> <input type="number" step="0.01" class="form-control" id="recoveryAmount" required> </div>
                    <div class="mb-3"> <label class="form-label">Payment Method </label> <select class="form-select" id="recoveryMethod"> <option>Cash</option> <option>Bank</option> <option>Cheque</option> <option>Easypaisa</option> <option>JazzCash</option> </select> </div>
                    <div class="mb-3"> <label class="form-label">Reference Number </label> <input type="text" class="form-control" id="recoveryRef"> </div>
                </form> </div>
                <div class="modal-footer"> <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button> <button type="button" class="btn btn-primary" onclick="submitRecovery()"> <i class="fa-solid fa-check me-1"></i>Save</button> </div>
            </div> </div>
        </div>`;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    document.getElementById('recoveryDate').valueAsDate = new Date();
    var modal = new bootstrap.Modal(document.getElementById('recoveryModal'));
    modal.show();
    setTimeout(() => document.getElementById('recoveryCustomer').focus(), 300);
    document.getElementById('recoveryModal').addEventListener('hidden.bs.modal', function () { this.remove(); });
}

function submitRecovery() {
    var cid = document.getElementById('recoveryCustomer').value;
    var date = document.getElementById('recoveryDate').value;
    var amt = document.getElementById('recoveryAmount').value;
    var method = document.getElementById('recoveryMethod').value;
    var ref = document.getElementById('recoveryRef').value;
    if (!cid || !amt) { alert('Customer and Amount are required!'); return; }
    fetch('/api/recoveries', {
        method: 'POST', headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerID: cid, RecoveryDate: date, AmountReceived: amt, PaymentMethod: method, ReferenceNumber: ref, Remarks: '' })
    }).then(r => r.json()).then(res => {
        bootstrap.Modal.getInstance(document.getElementById('recoveryModal')).hide();
        if(res.success) { alert('Recovery added successfully!'); loadPage('recoveries', document.querySelectorAll('.nav-link')[3]); }
        else { alert('Error: ' + res.message); }
    }).catch(err => alert('Error: ' + err));
}

function loadClosed(c) {
    c.innerHTML = `<div class="container-fluid"> 
        <h3 class="page-title"><i class="fa-solid fa-lock"></i> Closed Khatas</h3> 
        <div class="card"> 
            <div class="card-header"><i class="fa-solid fa-archive"></i> Archived / Closed Customer Accounts</div> 
            <div class="card-body"> 
                <table id="closedTable" class="table table-striped table-bordered" style="width:100%"> 
                    <thead><tr><th>Code</th><th>Name</th><th>Mobile</th><th>City</th><th>Final Balance</th><th>Actions</th></tr></thead> 
                    <tbody></tbody> 
                </table> 
            </div> 
        </div> 
    </div>`;
    fetch('/api/closed-customers').then(r => r.json()).then(data => {
        if(data.error) { alert('Error: ' + data.error); return; }
        var tb = $('#closedTable tbody'); tb.empty();
        data.forEach(x => {
            var cls = x.CurrentBalance > 0 ? 'text-danger fw-bold' : (x.CurrentBalance < 0 ? 'text-success fw-bold' : '');
            var row = `<tr> 
                <td><strong>${x.CustomerCode}</strong></td> 
                <td>${x.CustomerName}</td> 
                <td>${x.Mobile || '-'}</td> 
                <td>${x.City || '-'}</td> 
                <td class="${cls}">Rs. ${parseFloat(x.CurrentBalance).toFixed(2)}</td> 
                <td><button class="btn btn-sm btn-success" onclick="reopenKhata(${x.CustomerID})"><i class="fa-solid fa-rotate-left me-1"></i>Reopen</button></td> 
            </tr>`;
            tb.append(row);
        });
        if(currentTable) { currentTable.destroy(); }
        currentTable = $('#closedTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(err => alert('Error: ' + err));
}

function reopenKhata(id) {
    if(!confirm('Are you sure you want to reopen this khata?')) return;
    fetch('/api/customers/reopen', {
        method: 'POST', headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerID: id })
    }).then(r => r.json()).then(res => {
        if(res.success) { alert('Khata reopened successfully!'); loadPage('closed', document.querySelectorAll('.nav-link')[4]); }
        else { alert('Error: ' + res.message); }
    }).catch(err => alert('Error: ' + err));
}

// ============ ADMIN PANEL ============
function loadAdmin(c) {
    if (currentUserRole !== 'Admin') {
        c.innerHTML = '<div class="alert alert-danger"> <i class="fa-solid fa-shield-halved me-2"></i>Access Denied! Admin privileges required.</div>';
        return;
    }
    c.innerHTML = `
        <div class="container-fluid">
            <h3 class="page-title"> <i class="fa-solid fa-user-shield"></i> Admin Panel </h3>
            <div class="admin-section">
                <h5> <i class="fa-solid fa-server"></i> System Information </h5>
                <div class="row g-3" id="systemInfo">
                    <div class="col-md-3"> <div class="card p-3 text-center"> <small class="text-muted">Server Status</small> <h5 class="text-success mb-0 mt-2"> <i class="fa-solid fa-circle-check"></i> Online </h5> </div> </div>
                    <div class="col-md-3"> <div class="card p-3 text-center"> <small class="text-muted">Database</small> <h5 class="mb-0 mt-2" id="dbStatus">Loading...</h5> </div> </div>
                    <div class="col-md-3"> <div class="card p-3 text-center"> <small class="text-muted">Total Users</small> <h5 class="mb-0 mt-2" id="totalUsers">...</h5> </div> </div>
                    <div class="col-md-3"> <div class="card p-3 text-center"> <small class="text-muted">Server Time</small> <h5 class="mb-0 mt-2" id="serverTime">...</h5> </div> </div>
                </div>
            </div>
            <div class="admin-section">
                <h5> <i class="fa-solid fa-key"></i> Change Login Credentials </h5>
                <form id="passwordForm" onsubmit="changePassword(event)">
                    <div class="row g-3">
                        <div class="col-md-4">
                            <label class="form-label">Select User</label>
                            <select class="form-select" id="pwdUser" required></select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Current Password</label>
                            <input type="password" class="form-control" id="pwdCurrent" required>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">New Password</label>
                            <input type="password" class="form-control" id="pwdNew" required minlength="4">
                        </div>
                    </div>
                    <div class="row g-3 mt-2">
                        <div class="col-md-4">
                            <label class="form-label">New Username (optional)</label>
                            <input type="text" class="form-control" id="pwdNewUsername" placeholder="Leave blank to keep current">
                        </div>
                        <div class="col-md-4 d-flex align-items-end">
                            <button type="submit" class="btn btn-primary"> <i class="fa-solid fa-save me-1"></i> Update Credentials </button>
                        </div>
                    </div>
                </form>
            </div>
            <div class="admin-section">
                <h5> <i class="fa-solid fa-users-gear"></i> User Accounts </h5>
                <table class="table table-bordered">
                    <thead class="table-dark"> <tr> <th>ID</th> <th>Username</th> <th>Role</th> <th>Created</th> </tr> </thead>
                    <tbody id="usersTableBody"></tbody>
                </table>
            </div>
            <div class="admin-section">
                <h5> <i class="fa-solid fa-screwdriver-wrench"></i> Database Maintenance </h5>
                <p class="text-muted small">Use these options carefully. These actions cannot be undone.</p>
                <div class="row g-3">
                    <div class="col-md-4">
                        <div class="card p-3 border-warning">
                            <h6> <i class="fa-solid fa-broom text-warning me-1"></i> Clear All Transactions </h6>
                            <p class="small text-muted mb-2">Delete all Debit & Recovery entries. Customers will remain.</p>
                            <button class="btn btn-warning btn-sm" onclick="clearTransactions()"> <i class="fa-solid fa-broom me-1"></i>Clear Transactions</button>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card p-3 border-danger">
                            <h6> <i class="fa-solid fa-trash text-danger me-1"></i> Reset All Customers </h6>
                            <p class="small text-muted mb-2">Delete all customers and their transactions. Cannot be undone!</p>
                            <button class="btn btn-danger btn-sm" onclick="resetCustomers()"> <i class="fa-solid fa-trash me-1"></i>Reset Customers</button>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card p-3 border-info">
                            <h6> <i class="fa-solid fa-database text-info me-1"></i> Backup Info </h6>
                            <p class="small text-muted mb-2">Database: <strong>KhatifyDB</strong><br>Server: Local SQL Server</p>
                            <button class="btn btn-info btn-sm text-white" onclick="alert('Backup your KhatifyDB database using SQL Server Management Studio.')"> <i class="fa-solid fa-download me-1"></i>Backup Guide</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>`;
    loadAdminData();
}

function loadAdminData() {
    fetch('/api/admin/system-info').then(r => r.json()).then(d => {
        if (d.error) return;
        document.getElementById('dbStatus').innerHTML = '<span class="text-success"><i class="fa-solid fa-circle-check"></i> Connected</span>';
        document.getElementById('totalUsers').innerText = d.totalUsers;
        document.getElementById('serverTime').innerText = d.serverTime;
        var selectHtml = '';
        var tableHtml = '';
        d.users.forEach(u => {
            selectHtml += `<option value="${u.UserID}">${u.Username} (${u.Role})</option>`;
            tableHtml += `<tr> <td>${u.UserID}</td> <td><strong>${u.Username}</strong></td> <td><span class="badge bg-${u.Role === 'Admin' ? 'danger' : 'primary'}">${u.Role}</span></td> <td>${u.CreatedDate}</td> </tr>`;
        });
        document.getElementById('pwdUser').innerHTML = selectHtml;
        document.getElementById('usersTableBody').innerHTML = tableHtml;
    }).catch(err => console.error(err));
}

function changePassword(e) {
    e.preventDefault();
    var userId = document.getElementById('pwdUser').value;
    var currentPwd = document.getElementById('pwdCurrent').value;
    var newPwd = document.getElementById('pwdNew').value;
    var newUsername = document.getElementById('pwdNewUsername').value;
    fetch('/api/admin/change-password', {
        method: 'POST', headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ UserID: userId, CurrentPassword: currentPwd, NewPassword: newPwd, NewUsername: newUsername })
    }).then(r => r.json()).then(res => {
        if (res.success) { 
            alert('Credentials updated successfully!'); 
            document.getElementById('passwordForm').reset();
            loadAdminData();
        } else { alert('Error: ' + res.message); }
    }).catch(err => alert('Error: ' + err));
}

function clearTransactions() {
    if(!confirm('⚠️ WARNING: This will delete ALL Debit and Recovery entries!\n\nCustomers will remain but their balances will be reset.\n\nAre you absolutely sure?')) return;
    if(prompt('Type "DELETE" to confirm clearing all transactions:') !== 'DELETE') {
        alert('Cancelled.');
        return;
    }
    fetch('/api/admin/clear-transactions', { method: 'POST' })
    .then(r => r.json()).then(res => {
        if(res.success) { alert('All transactions cleared successfully!'); loadAdminData(); }
        else alert('Error: ' + res.message);
    }).catch(err => alert('Error: ' + err));
}

function resetCustomers() {
    if(!confirm('⚠️⚠️ DANGER: This will delete ALL customers, transactions, and ledger entries!\n\nThis action CANNOT be undone!\n\nAre you absolutely sure?')) return;
    if(prompt('Type "RESET ALL" to confirm total database reset:') !== 'RESET ALL') {
        alert('Cancelled.');
        return;
    }
    fetch('/api/admin/reset-all', { method: 'POST' })
    .then(r => r.json()).then(res => {
        if(res.success) { alert('Database reset successfully!'); loadAdminData(); }
        else alert('Error: ' + res.message);
    }).catch(err => alert('Error: ' + err));
}

function showLedger(cid) {
    fetch('/api/ledger?customerID=' + cid).then(r => r.json()).then(data => {
        if (data.error) { alert('Error: ' + data.error); return; }
        var html = '<h5 class="mb-3">Customer Ledger</h5><table class="table table-sm table-bordered"><thead class="table-dark"><tr><th>Date</th><th>Type</th><th>Description</th><th>Debit</th><th>Credit</th><th>Balance</th></tr></thead><tbody>';
        data.forEach(x => {
            html += `<tr><td>${x.TransactionDate}</td><td>${x.VoucherType}</td><td>${x.Description}</td>`;
            html += `<td>${x.DebitAmount > 0 ? 'Rs. ' + parseFloat(x.DebitAmount).toFixed(2) : '-'}</td>`;
            html += `<td>${x.CreditAmount > 0 ? 'Rs. ' + parseFloat(x.CreditAmount).toFixed(2) : '-'}</td>`;
            html += `<td><strong>Rs. ${parseFloat(x.RunningBalance).toFixed(2)}</strong></td></tr>`;
        });
        html += '</tbody></table>';
        var w = window.open('', '_blank', 'width=800,height=600');
        w.document.write('<html><head><title>Customer Ledger</title><link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet"><link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet"></head><body><div class="p-4">' + html + '</div></body></html>');
        w.document.close();
    }).catch(err => alert('Error loading ledger: ' + err));
}

window.onload = function() {
    fetch('/api/customers').then(r => r.ok ? r.json() : []).then(d => { allCustomers = Array.isArray(d) ? d : []; }).catch(() => { allCustomers = []; });
    loadPage('dashboard', document.querySelector('.nav-link'));
};