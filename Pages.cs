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
        <h2 class='text-center mb-4'>📊 Digital Khata</h2>
        <form id='loginForm'>
            <div class='mb-3'>
                <label>Username</label>
                <input type='text' id='username' class='form-control' value='admin' required>
            </div>
            <div class='mb-3'>
                <label>Password</label>
                <input type='password' id='password' class='form-control' value='admin123' required>
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
        <h3 class='mb-4'>📊 Digital Khata</h3>
        <hr>
        <div class='nav flex-column'>
            <div class='nav-item'><a class='nav-link active' onclick='loadPage(\"dashboard\", this)'>📈 Dashboard</a></div>
            <div class='nav-item'><a class='nav-link' onclick='loadPage(\"customers\", this)'> Customers</a></div>
            <div class='nav-item'><a class='nav-link' onclick='loadPage(\"debits\", this)'>💰 Debit Entries</a></div>
            <div class='nav-item'><a class='nav-link' onclick='loadPage(\"recoveries\", this)'>💵 Recovery Entries</a></div>
        </div>
        <hr>
        <a href='/login' class='btn btn-danger w-100'>Logout</a>
    </div>
    
    <div class='main' id='content'>
        <div class='text-center mt-5'><div class='spinner-border text-primary'></div><p class='mt-2'>Loading...</p></div>
    </div>

    <script src='https://code.jquery.com/jquery-3.7.0.min.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js'></script>
    <script src='https://cdn.datatables.net/1.13.6/js/jquery.dataTables.min.js'></script>
    <script src='https://cdn.datatables.net/1.13.6/js/dataTables.bootstrap5.min.js'></script>
    <script src='/js/app.js'></script>
</body>
</html>";
        }

        // ==================== DASHBOARD PAGE ====================
        public static string GetDashboardPage()
        {
            return @"<div class='container-fluid'>
    <h3 class='mb-4'>📈 Dashboard</h3>
    <div class='row g-3' id='dashboardStats'>
        <div class='col-md-3'>
            <div class='card stat-card bg-primary text-white p-3'>
                <h6>Total Customers</h6>
                <h2 id='statCustomers'>0</h2>
            </div>
        </div>
        <div class='col-md-3'>
            <div class='card stat-card bg-danger text-white p-3'>
                <h6>Total Debit</h6>
                <h2 id='statDebit'>Rs. 0.00</h2>
            </div>
        </div>
        <div class='col-md-3'>
            <div class='card stat-card bg-success text-white p-3'>
                <h6>Total Recovery</h6>
                <h2 id='statRecovery'>Rs. 0.00</h2>
            </div>
        </div>
        <div class='col-md-3'>
            <div class='card stat-card bg-warning text-dark p-3'>
                <h6>Outstanding</h6>
                <h2 id='statOutstanding'>Rs. 0.00</h2>
            </div>
        </div>
    </div>
</div>";
        }

        // ==================== CUSTOMERS PAGE ====================
        public static string GetCustomersPage()
        {
            return @"<div class='container-fluid'>
    <div class='d-flex justify-content-between align-items-center mb-4'>
        <h3>👥 Customers</h3>
        <button class='btn btn-primary' onclick='showAddCustomer()'>+ Add New Customer</button>
    </div>
    <div class='card'>
        <div class='card-body'>
            <table id='customersTable' class='table table-striped table-bordered' style='width:100%'>
                <thead class='table-dark'>
                    <tr>
                        <th>Code</th>
                        <th>Name</th>
                        <th>Mobile</th>
                        <th>City</th>
                        <th>Balance</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
        </div>
    </div>
</div>";
        }

        // ==================== DEBIT ENTRIES PAGE ====================
        public static string GetDebitsPage()
        {
            return @"<div class='container-fluid'>
    <div class='d-flex justify-content-between align-items-center mb-4'>
        <h3>💰 Debit Entries (Udhaar)</h3>
        <button class='btn btn-primary' onclick='showAddDebit()'>+ New Debit Entry</button>
    </div>
    <div class='card'>
        <div class='card-body'>
            <table id='debitsTable' class='table table-striped table-bordered' style='width:100%'>
                <thead class='table-dark'>
                    <tr>
                        <th>Date</th>
                        <th>Customer</th>
                        <th>Invoice #</th>
                        <th>Amount</th>
                        <th>Description</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
        </div>
    </div>
</div>";
        }

        // ==================== RECOVERY ENTRIES PAGE ====================
        public static string GetRecoveriesPage()
        {
            return @"<div class='container-fluid'>
    <div class='d-flex justify-content-between align-items-center mb-4'>
        <h3>💵 Recovery Entries (Vasooli)</h3>
        <button class='btn btn-primary' onclick='showAddRecovery()'>+ New Recovery Entry</button>
    </div>
    <div class='card'>
        <div class='card-body'>
            <table id='recoveriesTable' class='table table-striped table-bordered' style='width:100%'>
                <thead class='table-dark'>
                    <tr>
                        <th>Date</th>
                        <th>Customer</th>
                        <th>Amount</th>
                        <th>Method</th>
                        <th>Reference</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
        </div>
    </div>
</div>";
        }
    }
}