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

function loadDashboard(c) {
    fetch('/api/dashboard').then(function(r) { return r.json(); }).then(function(d) {
        if (d.error) {
            c.innerHTML = '<div class="alert alert-danger">Error: ' + d.error + '</div>';
            return;
        }
        document.getElementById('statCustomers').innerText = d.customers;
        document.getElementById('statDebit').innerText = 'Rs. ' + parseFloat(d.totalDebit).toFixed(2);
        document.getElementById('statRecovery').innerText = 'Rs. ' + parseFloat(d.totalRecovery).toFixed(2);
        document.getElementById('statOutstanding').innerText = 'Rs. ' + parseFloat(d.outstanding).toFixed(2);
    }).catch(function(err) {
        c.innerHTML = '<div class="alert alert-danger">Error loading dashboard: ' + err + '</div>';
        console.error('Dashboard error:', err);
    });
}

function loadCustomers(c) {
    c.innerHTML = '<div class="container-fluid"><div class="d-flex justify-content-between align-items-center mb-4"><h3>👥 Customers</h3><button class="btn btn-primary" onclick="showAddCustomer()">+ Add New Customer</button></div><div class="card"><div class="card-body"><table id="customersTable" class="table table-striped table-bordered" style="width:100%"><thead class="table-dark"><tr><th>Code</th><th>Name</th><th>Mobile</th><th>City</th><th>Balance</th><th>Actions</th></tr></thead><tbody></tbody></table></div></div></div>';
    
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
            row += '<td class="' + cls + '">Rs. ' + parseFloat(x.CurrentBalance).toFixed(2) + '</td>';
            row += '<td><button class="btn btn-sm btn-info" onclick="showLedger(' + x.CustomerID + ')">Ledger</button></td>';
            row += '</tr>';
            tb.append(row);
        });
        if (currentTable) { currentTable.destroy(); }
        currentTable = $('#customersTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(function(err) {
        alert('Error loading customers: ' + err);
        console.error('Customers error:', err);
    });
}

function showAddCustomer() {
    var code = prompt('Customer Code:');
    if (!code) return;
    var name = prompt('Customer Name:');
    if (!name) return;
    var mob = prompt('Mobile:');
    var city = prompt('City:');
    
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
    }).catch(function(err) {
        alert('Error: ' + err);
    });
}

function loadDebits(c) {
    c.innerHTML = '<div class="container-fluid"><div class="d-flex justify-content-between align-items-center mb-4"><h3>💰 Debit Entries (Udhaar)</h3><button class="btn btn-primary" onclick="showAddDebit()">+ New Debit Entry</button></div><div class="card"><div class="card-body"><table id="debitsTable" class="table table-striped table-bordered" style="width:100%"><thead class="table-dark"><tr><th>Date</th><th>Customer</th><th>Invoice #</th><th>Amount</th><th>Description</th></tr></thead><tbody></tbody></table></div></div></div>';
    
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
        if (currentTable) { currentTable.destroy(); }
        currentTable = $('#debitsTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(function(err) {
        alert('Error loading debits: ' + err);
        console.error('Debits error:', err);
    });
}

function showAddDebit() {
    if (allCustomers.length === 0) { alert('No customers! Add customer first.'); return; }
    var custList = allCustomers.map(function(x, i) { return (i+1) + '. ' + x.CustomerName + ' (Bal: Rs.' + parseFloat(x.CurrentBalance).toFixed(2) + ')'; }).join('\n');
    var custNum = prompt('Select Customer:\n' + custList + '\n\nEnter number:');
    if (!custNum) return;
    var idx = parseInt(custNum) - 1;
    if (idx < 0 || idx >= allCustomers.length) { alert('Invalid selection!'); return; }
    var cust = allCustomers[idx];
    var inv = prompt('Invoice Number:');
    var amt = prompt('Amount:');
    var desc = prompt('Description:');
    
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
    }).catch(function(err) {
        alert('Error: ' + err);
    });
}

function loadRecoveries(c) {
    c.innerHTML = '<div class="container-fluid"><div class="d-flex justify-content-between align-items-center mb-4"><h3>💵 Recovery Entries (Vasooli)</h3><button class="btn btn-primary" onclick="showAddRecovery()">+ New Recovery Entry</button></div><div class="card"><div class="card-body"><table id="recoveriesTable" class="table table-striped table-bordered" style="width:100%"><thead class="table-dark"><tr><th>Date</th><th>Customer</th><th>Amount</th><th>Method</th><th>Reference</th></tr></thead><tbody></tbody></table></div></div></div>';
    
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
        if (currentTable) { currentTable.destroy(); }
        currentTable = $('#recoveriesTable').DataTable({ pageLength: 10, order: [[0, 'desc']] });
    }).catch(function(err) {
        alert('Error loading recoveries: ' + err);
        console.error('Recoveries error:', err);
    });
}

function showAddRecovery() {
    if (allCustomers.length === 0) { alert('No customers!'); return; }
    var custList = allCustomers.map(function(x, i) { return (i+1) + '. ' + x.CustomerName; }).join('\n');
    var custNum = prompt('Select Customer:\n' + custList + '\n\nEnter number:');
    if (!custNum) return;
    var idx = parseInt(custNum) - 1;
    if (idx < 0 || idx >= allCustomers.length) { alert('Invalid selection!'); return; }
    var cust = allCustomers[idx];
    var amt = prompt('Amount Received:');
    var method = prompt('Payment Method (Cash/Bank/Cheque/Easypaisa/JazzCash):', 'Cash');
    var ref = prompt('Reference Number:');
    
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
    }).catch(function(err) {
        alert('Error: ' + err);
    });
}

function showLedger(cid) {
    fetch('/api/ledger?customerID=' + cid).then(function(r) { return r.json(); }).then(function(data) {
        if (data.error) { alert('Error: ' + data.error); return; }
        var html = '<h5>Customer Ledger</h5>';
        html += '<table class="table table-sm table-bordered"><thead class="table-light"><tr><th>Date</th><th>Type</th><th>Description</th><th>Debit</th><th>Credit</th><th>Balance</th></tr></thead><tbody>';
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
        w.document.write('<html><head><title>Customer Ledger</title><link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet"></head><body><div class="p-4">' + html + '</div></body></html>');
        w.document.close();
    }).catch(function(err) {
        alert('Error loading ledger: ' + err);
        console.error('Ledger error:', err);
    });
}

// Initialize
window.onload = function() {
    fetch('/api/customers').then(function(r) { 
        if (r.ok) return r.json(); 
        return [];
    }).then(function(d) { 
        allCustomers = d; 
    }).catch(function() { allCustomers = []; });
    
    loadPage('dashboard', document.querySelector('.nav-link'));
};