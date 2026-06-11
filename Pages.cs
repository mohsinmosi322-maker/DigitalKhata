using System;

namespace DigitalKhata
{
    class Pages
    {
        // ==================== LOGIN PAGE ====================
        public static string GetLoginPage()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <title>Login - Digital Khata</title>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
    <style>
        body { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); height: 100vh; display: flex; align-items: center; justify-content: center; }
        .login-box { background: white; padding: 40px; border-radius: 15px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); width: 400px; }
    </style>
</head>
<body>
    <div class='login-box'>
        <h2 class='text-center mb-4'>&#x1F4CA; Digital Khata</h2>
        <form id='loginForm'>
            <div class='mb-3'>
                <label>Username</label>
                <input type='text' id='username' class='form-control' required>
            </div>
            <div class='mb-3'>
                <label>Password</label>
                <input type='password' id='password' class='form-control' required>
            </div>
            <button type='submit' class='btn btn-primary w-100'>Login</button>
            <div id='msg' class='mt-3 text-danger text-center'></div>
        </form>
    </div>
    <script>
        document.getElementById('loginForm').onsubmit = async function(e) {
            e.preventDefault();
            var res = await fetch('/api/login', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({
                    username: document.getElementById('username').value,
                    password: document.getElementById('password').value
                })
            });
            var data = await res.json();
            if (data.success) window.location.href = '/app';
            else document.getElementById('msg').innerText = data.message;
        };
    </script>
</body>
</html>";
        }

        // ==================== MAIN APP PAGE ====================
        public static string GetAppPage()
        {
            // FIX: Dashboard HTML is now injected at page load so statCustomers/statDebit/etc.
            //      elements exist in the DOM when loadDashboard() runs fetch callbacks.
            //      Previously the content div was empty on load, so getElementById returned null.
            return @"<!DOCTYPE html>
<html>
<head>
    <title>Digital Khata</title>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
    <link href='https://cdn.datatables.net/1.13.6/css/dataTables.bootstrap5.min.css' rel='stylesheet'>
    <style>
        .sidebar { width: 260px; height: 100vh; position: fixed; left: 0; top: 0; background: #1a1d29; color: white; padding: 20px; overflow-y: auto; }
        .main { margin-left: 260px; padding: 20px; background: #f5f7fa; min-height: 100vh; }
        .nav-item { margin-bottom: 5px; }
        .nav-link { color: #a0aec0; cursor: pointer; padding: 12px 15px; border-radius: 8px; }
        .nav-link:hover, .nav-link.active { background: #2d3748; color: white; }
        .stat-card { border-radius: 12px; border: none; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }
        .btn-primary { background: #4f46e5; border: none; }
        .btn-primary:hover { background: #4338ca; }
    </style>
</head>
<body>
    <div class='sidebar'>
        <h3 class='mb-4'>&#x1F4CA; Digital Khata</h3>
        <hr>
        <div class='nav flex-column'>
            <div class='nav-item'><a class='nav-link active' onclick='loadPage(""dashboard"", this)'>&#x1F4C8; Dashboard</a></div>
            <div class='nav-item'><a class='nav-link' onclick='loadPage(""customers"", this)'>&#x1F465; Customers</a></div>
            <div class='nav-item'><a class='nav-link' onclick='loadPage(""debits"", this)'>&#x1F4B0; Debit Entries</a></div>
            <div class='nav-item'><a class='nav-link' onclick='loadPage(""recoveries"", this)'>&#x1F4B5; Recovery Entries</a></div>
        </div>
        <hr>
        <a href='/login' class='btn btn-danger w-100'>Logout</a>
    </div>

    <div class='main' id='content'>
        <!-- FIX: Dashboard HTML pre-loaded here so stat element IDs exist on first fetch -->
        <div class='container-fluid'>
            <h3 class='mb-4'>&#x1F4C8; Dashboard</h3>
            <div class='row g-3'>
                <div class='col-md-3'>
                    <div class='card stat-card bg-primary text-white p-3'>
                        <h6>Total Customers</h6>
                        <h2 id='statCustomers'>...</h2>
                    </div>
                </div>
                <div class='col-md-3'>
                    <div class='card stat-card bg-danger text-white p-3'>
                        <h6>Total Debit</h6>
                        <h2 id='statDebit'>...</h2>
                    </div>
                </div>
                <div class='col-md-3'>
                    <div class='card stat-card bg-success text-white p-3'>
                        <h6>Total Recovery</h6>
                        <h2 id='statRecovery'>...</h2>
                    </div>
                </div>
                <div class='col-md-3'>
                    <div class='card stat-card bg-warning text-dark p-3'>
                        <h6>Outstanding</h6>
                        <h2 id='statOutstanding'>...</h2>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src='https://code.jquery.com/jquery-3.7.0.min.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js'></script>
    <script src='https://cdn.datatables.net/1.13.6/js/jquery.dataTables.min.js'></script>
    <script src='https://cdn.datatables.net/1.13.6/js/dataTables.bootstrap5.min.js'></script>
    <!-- FIX: Route /js/app.js is now handled by the server -->
    <script src='/js/app.js'></script>
</body>
</html>";
        }

        // ==================== APP JS (served at /js/app.js) ====================
        // FIX: app.js is now served from here so no external file dependency is needed.
        //      loadDashboard now injects dashboard HTML into content div before fetching,
        //      ensuring stat element IDs always exist when the fetch callback fires.
        public static string GetAppJs()
        {
            return @"
var currentTable = null;
var allCustomers = [];

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
}

// FIX: Dashboard HTML is now injected first so getElementById calls in the
//      fetch callback always find the stat elements in the DOM.
function loadDashboard(c) {
    c.innerHTML = '<div class=""container-fluid"">' +
        '<h3 class=""mb-4"">&#x1F4C8; Dashboard</h3>' +
        '<div class=""row g-3"">' +
        '<div class=""col-md-3""><div class=""card stat-card bg-primary text-white p-3""><h6>Total Customers</h6><h2 id=""statCustomers"">...</h2></div></div>' +
        '<div class=""col-md-3""><div class=""card stat-card bg-danger text-white p-3""><h6>Total Debit</h6><h2 id=""statDebit"">...</h2></div></div>' +
        '<div class=""col-md-3""><div class=""card stat-card bg-success text-white p-3""><h6>Total Recovery</h6><h2 id=""statRecovery"">...</h2></div></div>' +
        '<div class=""col-md-3""><div class=""card stat-card bg-warning text-dark p-3""><h6>Outstanding</h6><h2 id=""statOutstanding"">...</h2></div></div>' +
        '</div></div>';

    fetch('/api/dashboard').then(function(r) { return r.json(); }).then(function(d) {
        if (d.error) {
            c.innerHTML = '<div class=""alert alert-danger"">Error: ' + d.error + '</div>';
            return;
        }
        document.getElementById('statCustomers').innerText = d.customers;
        document.getElementById('statDebit').innerText = 'Rs. ' + parseFloat(d.totalDebit).toFixed(2);
        document.getElementById('statRecovery').innerText = 'Rs. ' + parseFloat(d.totalRecovery).toFixed(2);
        document.getElementById('statOutstanding').innerText = 'Rs. ' + parseFloat(d.outstanding).toFixed(2);
    }).catch(function(err) {
        c.innerHTML = '<div class=""alert alert-danger"">Error loading dashboard: ' + err + '</div>';
    });
}

function loadCustomers(c) {
    c.innerHTML = '<div class=""container-fluid""><div class=""d-flex justify-content-between align-items-center mb-4""><h3>&#x1F465; Customers</h3><button class=""btn btn-primary"" onclick=""showAddCustomer()"">+ Add New Customer</button></div><div class=""card""><div class=""card-body""><table id=""customersTable"" class=""table table-striped table-bordered"" style=""width:100%""><thead class=""table-dark""><tr><th>Code</th><th>Name</th><th>Mobile</th><th>City</th><th>Balance</th><th>Actions</th></tr></thead><tbody></tbody></table></div></div></div>';

    fetch('/api/customers').then(function(r) { return r.json(); }).then(function(data) {
        if (data.error) { alert('Error: ' + data.error); return; }
        allCustomers = data;
        var tb = $('#customersTable tbody');
        tb.empty();
        data.forEach(function(x) {
            var cls = x.CurrentBalance > 0 ? 'text-danger fw-bold' : 'text-success';
            var row = '<tr>';
            row += '<td>' + x.CustomerCode + '</td>';
            row += '<td>' + x.CustomerName + '</td>';
            row += '<td>' + x.Mobile + '</td>';
            row += '<td>' + x.City + '</td>';
            row += '<td class=""' + cls + '"">Rs. ' + parseFloat(x.CurrentBalance).toFixed(2) + '</td>';
            row += '<td><button class=""btn btn-sm btn-info"" onclick=""showLedger(' + x.CustomerID + ')"">Ledger</button></td>';
            row += '</tr>';
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); currentTable = null; }
        currentTable = $('#customersTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(function(err) {
        alert('Error loading customers: ' + err);
    });
}

function showAddCustomer() {
    var code = prompt('Customer Code:');
    if (!code) return;
    var name = prompt('Customer Name:');
    if (!name) return;
    var mob = prompt('Mobile:') || '';
    var city = prompt('City:') || '';

    fetch('/api/customers', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerCode: code, CustomerName: name, Mobile: mob, City: city, OpeningBalance: 0 })
    }).then(function(r) { return r.json(); }).then(function(res) {
        if (res.success) {
            alert('Customer added successfully!');
            loadPage('customers', document.querySelectorAll('.nav-link')[1]);
        } else {
            alert('Error: ' + res.message);
        }
    }).catch(function(err) { alert('Error: ' + err); });
}

function loadDebits(c) {
    c.innerHTML = '<div class=""container-fluid""><div class=""d-flex justify-content-between align-items-center mb-4""><h3>&#x1F4B0; Debit Entries (Udhaar)</h3><button class=""btn btn-primary"" onclick=""showAddDebit()"">+ New Debit Entry</button></div><div class=""card""><div class=""card-body""><table id=""debitsTable"" class=""table table-striped table-bordered"" style=""width:100%""><thead class=""table-dark""><tr><th>Date</th><th>Customer</th><th>Invoice #</th><th>Amount</th><th>Description</th></tr></thead><tbody></tbody></table></div></div></div>';

    fetch('/api/debits').then(function(r) { return r.json(); }).then(function(data) {
        if (data.error) { alert('Error: ' + data.error); return; }
        var tb = $('#debitsTable tbody');
        tb.empty();
        data.forEach(function(x) {
            var row = '<tr>';
            row += '<td>' + x.DebitDate + '</td>';
            row += '<td>' + x.CustomerName + '</td>';
            row += '<td>' + x.InvoiceNumber + '</td>';
            row += '<td>Rs. ' + parseFloat(x.Amount).toFixed(2) + '</td>';
            row += '<td>' + x.Description + '</td>';
            row += '</tr>';
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); currentTable = null; }
        currentTable = $('#debitsTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(function(err) { alert('Error loading debits: ' + err); });
}

function showAddDebit() {
    if (allCustomers.length === 0) { alert('No customers! Add customer first.'); return; }
    var custList = allCustomers.map(function(x, i) { return (i+1) + '. ' + x.CustomerName + ' (Bal: Rs.' + parseFloat(x.CurrentBalance).toFixed(2) + ')'; }).join('\n');
    var custNum = prompt('Select Customer:\n' + custList + '\n\nEnter number:');
    if (!custNum) return;
    var idx = parseInt(custNum) - 1;
    if (isNaN(idx) || idx < 0 || idx >= allCustomers.length) { alert('Invalid selection!'); return; }
    var cust = allCustomers[idx];
    var inv = prompt('Invoice Number:') || '';
    var amt = prompt('Amount:');
    // FIX: Validate that amount is a positive number before submitting
    if (!amt || isNaN(parseFloat(amt)) || parseFloat(amt) <= 0) { alert('Invalid amount!'); return; }
    var desc = prompt('Description:') || '';

    fetch('/api/debits', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerID: cust.CustomerID, InvoiceNumber: inv, Amount: amt, Description: desc })
    }).then(function(r) { return r.json(); }).then(function(res) {
        if (res.success) {
            alert('Debit added successfully!');
            loadPage('debits', document.querySelectorAll('.nav-link')[2]);
        } else {
            alert('Error: ' + res.message);
        }
    }).catch(function(err) { alert('Error: ' + err); });
}

function loadRecoveries(c) {
    c.innerHTML = '<div class=""container-fluid""><div class=""d-flex justify-content-between align-items-center mb-4""><h3>&#x1F4B5; Recovery Entries (Vasooli)</h3><button class=""btn btn-primary"" onclick=""showAddRecovery()"">+ New Recovery Entry</button></div><div class=""card""><div class=""card-body""><table id=""recoveriesTable"" class=""table table-striped table-bordered"" style=""width:100%""><thead class=""table-dark""><tr><th>Date</th><th>Customer</th><th>Amount</th><th>Method</th><th>Reference</th></tr></thead><tbody></tbody></table></div></div></div>';

    fetch('/api/recoveries').then(function(r) { return r.json(); }).then(function(data) {
        if (data.error) { alert('Error: ' + data.error); return; }
        var tb = $('#recoveriesTable tbody');
        tb.empty();
        data.forEach(function(x) {
            var row = '<tr>';
            row += '<td>' + x.RecoveryDate + '</td>';
            row += '<td>' + x.CustomerName + '</td>';
            row += '<td>Rs. ' + parseFloat(x.AmountReceived).toFixed(2) + '</td>';
            row += '<td>' + x.PaymentMethod + '</td>';
            row += '<td>' + x.ReferenceNumber + '</td>';
            row += '</tr>';
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); currentTable = null; }
        currentTable = $('#recoveriesTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(function(err) { alert('Error loading recoveries: ' + err); });
}

function showAddRecovery() {
    if (allCustomers.length === 0) { alert('No customers!'); return; }
    var custList = allCustomers.map(function(x, i) { return (i+1) + '. ' + x.CustomerName; }).join('\n');
    var custNum = prompt('Select Customer:\n' + custList + '\n\nEnter number:');
    if (!custNum) return;
    var idx = parseInt(custNum) - 1;
    if (isNaN(idx) || idx < 0 || idx >= allCustomers.length) { alert('Invalid selection!'); return; }
    var cust = allCustomers[idx];
    var amt = prompt('Amount Received:');
    // FIX: Validate that amount is a positive number before submitting
    if (!amt || isNaN(parseFloat(amt)) || parseFloat(amt) <= 0) { alert('Invalid amount!'); return; }
    var method = prompt('Payment Method (Cash/Bank/Cheque/Easypaisa/JazzCash):', 'Cash') || 'Cash';
    var ref = prompt('Reference Number:') || '';

    fetch('/api/recoveries', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ CustomerID: cust.CustomerID, AmountReceived: amt, PaymentMethod: method, ReferenceNumber: ref, Remarks: '' })
    }).then(function(r) { return r.json(); }).then(function(res) {
        if (res.success) {
            alert('Recovery added successfully!');
            loadPage('recoveries', document.querySelectorAll('.nav-link')[3]);
        } else {
            alert('Error: ' + res.message);
        }
    }).catch(function(err) { alert('Error: ' + err); });
}

function showLedger(cid) {
    fetch('/api/ledger?customerID=' + cid).then(function(r) { return r.json(); }).then(function(data) {
        if (data.error) { alert('Error: ' + data.error); return; }
        var html = '<h5>Customer Ledger</h5>';
        html += '<table class=""table table-sm table-bordered""><thead class=""table-light""><tr><th>Date</th><th>Type</th><th>Description</th><th>Debit</th><th>Credit</th><th>Balance</th></tr></thead><tbody>';
        data.forEach(function(x) {
            html += '<tr>';
            html += '<td>' + x.TransactionDate + '</td>';
            html += '<td>' + x.VoucherType + '</td>';
            html += '<td>' + x.Description + '</td>';
            html += '<td>' + (x.DebitAmount > 0 ? 'Rs. ' + parseFloat(x.DebitAmount).toFixed(2) : '-') + '</td>';
            html += '<td>' + (x.CreditAmount > 0 ? 'Rs. ' + parseFloat(x.CreditAmount).toFixed(2) : '-') + '</td>';
            html += '<td><strong>Rs. ' + parseFloat(x.RunningBalance).toFixed(2) + '</strong></td>';
            html += '</tr>';
        });
        html += '</tbody></table>';
        var w = window.open('', '_blank', 'width=800,height=600');
        w.document.write('<html><head><title>Customer Ledger</title><link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet""></head><body><div class=""p-4"">' + html + '</div></body></html>');
        w.document.close();
    }).catch(function(err) { alert('Error loading ledger: ' + err); });
}

// Initialize
window.onload = function() {
    fetch('/api/customers').then(function(r) {
        if (r.ok) return r.json();
        return [];
    }).then(function(d) {
        allCustomers = Array.isArray(d) ? d : [];
    }).catch(function() { allCustomers = []; });

    loadPage('dashboard', document.querySelector('.nav-link'));
};
";
        }
    }
}