using System;

namespace Khatify
{
    class Pages
    {
        public static string GetLoginPage()
        {
            return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Login - Digital Khata</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
    <link href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" rel=""stylesheet"">
    <style>
        :root { --primary-dark: #121358; --accent: #36ADA3; }
        body { background: linear-gradient(135deg, #121358 0%, #232F72 100%); height: 100vh; display: flex; align-items: center; justify-content: center; font-family: 'Segoe UI', sans-serif; }
        .login-card { background: #fff; border-radius: 15px; box-shadow: 0 10px 30px rgba(0,0,0,0.3); width: 100%; max-width: 420px; overflow: hidden; }
        .login-header { background: var(--primary-dark); color: #fff; padding: 35px; text-align: center; }
        .login-header i { font-size: 3rem; color: var(--accent); margin-bottom: 10px; }
        .login-header h3 { margin: 0; font-weight: bold; }
        .login-header span { color: var(--accent); }
        .login-body { padding: 35px; }
        .form-control:focus { border-color: var(--accent); box-shadow: 0 0 0 0.25rem rgba(54, 173, 163, 0.25); }
        .btn-login { background: var(--accent); color: #fff; font-weight: bold; border: none; padding: 12px; border-radius: 8px; width: 100%; }
        .btn-login:hover { background: #2b8a82; color: #fff; }
        .input-group-text { background: #f8f9fa; border-right: 0; }
        .input-group .form-control { border-left: 0; }
    </style>
</head>
<body>
    <div class=""login-card"">
        <div class=""login-header"">
            <i class=""fa-solid fa-wallet""></i>
            <h3>Digital <span>Khata</span></h3>
            <p class=""mb-0 mt-2 opacity-75"">Debit & Recovery Management System</p>
        </div>
        <div class=""login-body"">
            <form onsubmit=""handleLogin(event)"">
                <div class=""mb-3"">
                    <label class=""form-label fw-bold""><i class=""fa-solid fa-user me-1""></i> Username</label>
                    <div class=""input-group"">
                        <span class=""input-group-text""><i class=""fa-solid fa-user""></i></span>
                        <input type=""text"" id=""username"" class=""form-control form-control-lg"" required>
                    </div>
                </div>
                <div class=""mb-4"">
                    <label class=""form-label fw-bold""><i class=""fa-solid fa-lock me-1""></i> Password</label>
                    <div class=""input-group"">
                        <span class=""input-group-text""><i class=""fa-solid fa-lock""></i></span>
                        <input type=""password"" id=""password"" class=""form-control form-control-lg"" required>
                    </div>
                </div>
                <button type=""submit"" class=""btn-login""><i class=""fa-solid fa-right-to-bracket me-2""></i>Login to Dashboard</button>
            </form>
        </div>
    </div>
    <script>
        function handleLogin(e) {
            e.preventDefault();
            fetch('/api/login', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ username: document.getElementById('username').value, password: document.getElementById('password').value })
            }).then(r => r.json()).then(d => {
                if (d.success) { 
                    localStorage.setItem('dk_role', d.role);
                    localStorage.setItem('dk_user', document.getElementById('username').value);
                    window.location.href = '/app'; 
                } else { alert(d.message); }
            });
        }
    </script>
</body>
</html>";
        }

        public static string GetAppPage()
        {
            return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Digital Khata</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
    <link href=""https://cdn.datatables.net/1.13.4/css/dataTables.bootstrap5.min.css"" rel=""stylesheet"">
    <link href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" rel=""stylesheet"">
    <script src=""https://code.jquery.com/jquery-3.6.0.min.js""></script>
    <script src=""https://cdn.datatables.net/1.13.4/js/jquery.dataTables.min.js""></script>
    <script src=""https://cdn.datatables.net/1.13.4/js/dataTables.bootstrap5.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
    <style>
        :root {
            --primary-dark: #121358;
            --primary-med: #232F72;
            --primary-light: #2F578A;
            --accent: #36ADA3;
            --bg-light: #F4F7F6;
            --text-dark: #121358;
        }
        body { background-color: var(--bg-light); color: var(--text-dark); font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        
        .navbar { background: linear-gradient(90deg, var(--primary-dark) 0%, var(--primary-med) 100%) !important; box-shadow: 0 4px 15px rgba(18, 19, 88, 0.25); padding: 12px 20px; }
        .navbar-brand { color: #fff !important; font-weight: bold; font-size: 1.4rem; letter-spacing: 1px; display: flex; align-items: center; gap: 10px; }
        .navbar-brand i { color: var(--accent); font-size: 1.6rem; }
        .navbar-brand span { color: var(--accent); }
        .nav-link { color: rgba(255,255,255,0.85) !important; font-weight: 500; margin: 0 3px; border-radius: 8px; transition: all 0.3s; display: flex; align-items: center; gap: 6px; padding: 8px 14px !important; }
        .nav-link i { font-size: 0.95rem; }
        .nav-link:hover { color: #fff !important; background-color: rgba(255,255,255,0.1); }
        .nav-link.active { background-color: var(--accent) !important; color: #fff !important; box-shadow: 0 2px 8px rgba(54, 173, 163, 0.4); }
        .nav-link.admin-link { background-color: rgba(255, 193, 7, 0.15); border: 1px solid rgba(255, 193, 7, 0.4); }
        .nav-link.admin-link:hover { background-color: rgba(255, 193, 7, 0.3); }
        .nav-link.admin-link.active { background-color: #ffc107 !important; color: var(--primary-dark) !important; }
        
        .btn-primary { background-color: var(--accent); border-color: var(--accent); color: #fff; font-weight: 600; }
        .btn-primary:hover { background-color: #2b8a82; border-color: #2b8a82; }
        .btn-outline-light { border-color: var(--accent); color: var(--accent); }
        .btn-outline-light:hover { background-color: var(--accent); color: #fff; }
        
        .card { border: none; box-shadow: 0 4px 15px rgba(18, 19, 88, 0.06); border-radius: 12px; overflow: hidden; transition: transform 0.2s; }
        .card:hover { transform: translateY(-2px); }
        .card-header { background: linear-gradient(90deg, var(--primary-med), var(--primary-light)); color: #fff; border-bottom: none; font-weight: 600; padding: 15px 20px; display: flex; align-items: center; gap: 10px; }
        .card-header i { font-size: 1.1rem; }
        .card-body { padding: 20px; }
        .table { color: var(--text-dark); }
        .table thead { background-color: var(--primary-dark); color: #fff; }
        .table thead th { border: none; font-weight: 600; text-transform: uppercase; font-size: 0.82rem; letter-spacing: 0.5px; padding: 12px; }
        .table tbody tr:hover { background-color: rgba(54, 173, 163, 0.05); }
        .table td { padding: 12px; vertical-align: middle; }
        
        .stat-card { border-left: 5px solid var(--accent); background: #fff; }
        .stat-card h2 { color: var(--primary-dark); font-weight: 700; font-size: 1.8rem; }
        .stat-card h5 { color: var(--primary-light); font-size: 0.85rem; text-transform: uppercase; font-weight: 600; margin-bottom: 8px; display: flex; align-items: center; gap: 8px; }
        .stat-card .stat-icon { font-size: 2rem; opacity: 0.3; position: absolute; right: 20px; top: 20px; }
        
        .modal-header { background: linear-gradient(90deg, var(--primary-dark), var(--primary-med)); color: #fff; }
        .modal-header .btn-close { filter: invert(1); }
        .form-control:focus, .form-select:focus { border-color: var(--accent); box-shadow: 0 0 0 0.25rem rgba(54, 173, 163, 0.25); }
        .form-label { font-weight: 600; color: var(--primary-med); }
        
        .dataTables_wrapper .dataTables_paginate .paginate_button.current { background: var(--accent) !important; color: #fff !important; border: 1px solid var(--accent) !important; border-radius: 5px; }
        .dataTables_wrapper .dataTables_paginate .paginate_button:hover { background: var(--primary-light) !important; color: #fff !important; border: 1px solid var(--primary-light) !important; border-radius: 5px; }
        
        .admin-section { background: #fff; border-radius: 12px; padding: 25px; margin-bottom: 20px; box-shadow: 0 4px 15px rgba(18, 19, 88, 0.06); }
        .admin-section h5 { color: var(--primary-dark); font-weight: 700; margin-bottom: 20px; display: flex; align-items: center; gap: 10px; padding-bottom: 12px; border-bottom: 2px solid var(--accent); }
        .admin-section h5 i { color: var(--accent); }
        
        .user-badge { background: var(--primary-med); color: #fff; padding: 6px 14px; border-radius: 20px; font-size: 0.85rem; display: inline-flex; align-items: center; gap: 6px; }
        .user-badge i { color: var(--accent); }
        
        .page-title { color: var(--primary-dark); font-weight: 700; display: flex; align-items: center; gap: 12px; margin-bottom: 25px; }
        .page-title i { color: var(--accent); }
    </style>
</head>
<body>
    <nav class=""navbar navbar-expand-lg"">
        <div class=""container-fluid"">
            <a class=""navbar-brand"" href=""#""><i class=""fa-solid fa-wallet""></i> Digital <span>Khata</span></a>
            <button class=""navbar-toggler"" type=""button"" data-bs-toggle=""collapse"" data-bs-target=""#navbarNav"">
                <span class=""navbar-toggler-icon""></span>
            </button>
            <div class=""collapse navbar-collapse"" id=""navbarNav"">
                <ul class=""navbar-nav me-auto mb-2 mb-lg-0"">
                    <li class=""nav-item""><a class=""nav-link active"" href=""#"" onclick=""loadPage('dashboard', this)""><i class=""fa-solid fa-chart-line""></i> Dashboard</a></li>
                    <li class=""nav-item""><a class=""nav-link"" href=""#"" onclick=""loadPage('customers', this)""><i class=""fa-solid fa-users""></i> Active Khatas</a></li>
                    <li class=""nav-item""><a class=""nav-link"" href=""#"" onclick=""loadPage('debits', this)""><i class=""fa-solid fa-file-invoice-dollar""></i> Debit Entries</a></li>
                    <li class=""nav-item""><a class=""nav-link"" href=""#"" onclick=""loadPage('recoveries', this)""><i class=""fa-solid fa-hand-holding-dollar""></i> Recovery Entries</a></li>
                    <li class=""nav-item""><a class=""nav-link"" href=""#"" onclick=""loadPage('closed', this)""><i class=""fa-solid fa-lock""></i> Closed Khatas</a></li>
                    <li class=""nav-item"" id=""adminMenuItem"" style=""display:none;""><a class=""nav-link admin-link"" href=""#"" onclick=""loadPage('admin', this)""><i class=""fa-solid fa-user-shield""></i> Admin Panel</a></li>
                </ul>
                <div class=""d-flex align-items-center gap-2"">
                    <span class=""user-badge"" id=""userBadge""><i class=""fa-solid fa-user-circle""></i> <span id=""userName"">User</span></span>
                    <a href=""/login"" class=""btn btn-outline-light btn-sm"" onclick=""localStorage.clear()""><i class=""fa-solid fa-right-from-bracket me-1""></i>Logout</a>
                </div>
            </div>
        </div>
    </nav>
    <div id=""content"" class=""p-4""></div>
    <script src=""/js/app.js""></script>
</body>
</html>";
        }
    }
}