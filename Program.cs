using System;
using System.IO;
using System.Net;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Khatify
{
    class Program
    {
        public static string connStr = "";
        public static JavaScriptSerializer json = new JavaScriptSerializer();

        static void Main(string[] args)
        {
            Console.Title = "Digital Khata Server";
            Console.WriteLine("===========================================");
            Console.WriteLine("   Digital Khata - Debit Recovery System    ");
            Console.WriteLine("===========================================\n");

            if (!FindSqlServer())
            {
                Console.WriteLine("\nSQL Server connection failed!");
                Console.ReadLine();
                return;
            }

            InitDB();
            StartServer();
        }

        static string GetLocalIP()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

        static bool TryConnect(string dataSource, out string errorMsg)
        {
            errorMsg = "";
            string testConn = @"Data Source=" + dataSource + ";Initial Catalog=master;Integrated Security=True;Connect Timeout=5;";
            try
            {
                using (SqlConnection conn = new SqlConnection(testConn))
                {
                    conn.Open();
                    connStr = testConn.Replace("Initial Catalog=master;", "Initial Catalog=KhatifyDB;");
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }

        static bool FindSqlServer()
        {
            Console.WriteLine("[1/3] Searching for SQL Server...");
            string[] instances = { @".\SQLEXPRESS", ".", "(local)", "localhost" };
            string errorMsg = "";
            foreach (string inst in instances)
            {
                Console.Write("  Trying " + inst + "... ");
                if (TryConnect(inst, out errorMsg)) { Console.WriteLine("OK"); return true; }
                Console.WriteLine("Failed");
            }
            Console.WriteLine("\n  Auto-detect failed. Enter SQL Server instance:");
            Console.Write("  (or press Enter for '.'): ");
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) input = ".";
            Console.Write("  Trying " + input + "... ");
            if (TryConnect(input, out errorMsg)) { Console.WriteLine("OK"); return true; }
            Console.WriteLine("Failed: " + errorMsg);
            return false;
        }

        static void InitDB()
        {
            Console.WriteLine("\n[2/3] Creating Database Tables...");
            string masterConn = connStr.Replace("KhatifyDB", "master");
            try
            {
                using (SqlConnection conn = new SqlConnection(masterConn))
                {
                    conn.Open();
                    new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'KhatifyDB') CREATE DATABASE KhatifyDB", conn).ExecuteNonQuery();
                }
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' and xtype='U')
                        CREATE TABLE Users (UserID INT IDENTITY(1,1) PRIMARY KEY, Username NVARCHAR(50) NOT NULL, Password NVARCHAR(50) NOT NULL, Role NVARCHAR(20) DEFAULT 'Operator', CreatedDate DATETIME DEFAULT GETDATE());
                        
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers' and xtype='U')
                        CREATE TABLE Customers (CustomerID INT IDENTITY(1,1) PRIMARY KEY, CustomerCode NVARCHAR(50) NOT NULL, CustomerName NVARCHAR(100) NOT NULL, FatherName NVARCHAR(100), Mobile NVARCHAR(20), CNIC NVARCHAR(20), Address NVARCHAR(250), City NVARCHAR(50), OpeningBalance DECIMAL(18,2) DEFAULT 0, CurrentBalance DECIMAL(18,2) DEFAULT 0, CreditLimit DECIMAL(18,2) DEFAULT 0, Status BIT DEFAULT 1, CreatedDate DATETIME DEFAULT GETDATE());
                        
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Debits' and xtype='U')
                        CREATE TABLE Debits (DebitID INT IDENTITY(1,1) PRIMARY KEY, DebitDate DATETIME DEFAULT GETDATE(), CustomerID INT FOREIGN KEY REFERENCES Customers(CustomerID), InvoiceNumber NVARCHAR(50), Amount DECIMAL(18,2) NOT NULL, Description NVARCHAR(250), CreatedDate DATETIME DEFAULT GETDATE());
                        
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Recoveries' and xtype='U')
                        CREATE TABLE Recoveries (RecoveryID INT IDENTITY(1,1) PRIMARY KEY, RecoveryDate DATETIME DEFAULT GETDATE(), CustomerID INT FOREIGN KEY REFERENCES Customers(CustomerID), AmountReceived DECIMAL(18,2) NOT NULL, PaymentMethod NVARCHAR(50), ReferenceNumber NVARCHAR(50), Remarks NVARCHAR(250), CreatedDate DATETIME DEFAULT GETDATE());
                        
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CustomerLedger' and xtype='U')
                        CREATE TABLE CustomerLedger (LedgerID INT IDENTITY(1,1) PRIMARY KEY, CustomerID INT FOREIGN KEY REFERENCES Customers(CustomerID), TransactionDate DATETIME NOT NULL, VoucherType NVARCHAR(20), ReferenceID INT, Description NVARCHAR(250), DebitAmount DECIMAL(18,2) DEFAULT 0, CreditAmount DECIMAL(18,2) DEFAULT 0, RunningBalance DECIMAL(18,2) DEFAULT 0, CreatedDate DATETIME DEFAULT GETDATE());
                        
                        IF NOT EXISTS (SELECT * FROM Users WHERE Username='admin')
                        INSERT INTO Users (Username, Password, Role) VALUES ('admin', 'admin123', 'Admin');
                    ";
                    new SqlCommand(sql, conn).ExecuteNonQuery();
                    Console.WriteLine("  Database tables created successfully!");
                }
            }
            catch (Exception ex) { Console.WriteLine("  Error: " + ex.Message); Console.ReadLine(); Environment.Exit(1); }
        }

        static void StartServer()
        {
            Console.WriteLine("\n[3/3] Starting Web Server...");
            HttpListener listener = new HttpListener();
            
            listener.Prefixes.Add("http://*:8080/");

            try
            {
                listener.Start();
                string localIP = GetLocalIP();
                Console.WriteLine("  ✅ Server Started Successfully!");
                Console.WriteLine("  🌐 Local Access: http://localhost:8080/");
                Console.WriteLine("  🌐 Network Access: http://" + localIP + ":8080/");
                Console.WriteLine("  📱 Share this IP with other devices on the same WiFi/LAN");
                Console.WriteLine("  ⚠️  Press Ctrl+C to stop\n");
                Process.Start("http://localhost:8080/");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  ❌ Error: " + ex.Message);
                Console.WriteLine("  🔒 Run as Administrator & Allow Port 8080 in Firewall!");
                Console.ReadLine();
                return;
            }

            while (true) { var ctx = listener.GetContext(); ThreadPool.QueueUserWorkItem((_) => HandleRequest(ctx)); }
        }

        static void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                string path = ctx.Request.Url.AbsolutePath;
                if (path == "/" || path == "/login") ServeHtml(ctx, Pages.GetLoginPage());
                else if (path == "/app") ServeHtml(ctx, Pages.GetAppPage());
                else if (path == "/js/app.js") { try { ServeJs(ctx, File.ReadAllText("app.js")); } catch { Serve500(ctx, "app.js not found"); } }
                
                else if (path == "/api/login" && ctx.Request.HttpMethod == "POST") Apis.ApiLogin(ctx);
                else if (path == "/api/dashboard") Apis.ApiDashboard(ctx);
                else if (path == "/api/customers" && ctx.Request.HttpMethod == "GET") Apis.ApiGetCustomers(ctx);
                else if (path == "/api/customers" && ctx.Request.HttpMethod == "POST") Apis.ApiAddCustomer(ctx);
                else if (path == "/api/customers/close" && ctx.Request.HttpMethod == "POST") Apis.ApiCloseCustomer(ctx);
                else if (path == "/api/customers/reopen" && ctx.Request.HttpMethod == "POST") Apis.ApiReopenCustomer(ctx);
                else if (path == "/api/closed-customers" && ctx.Request.HttpMethod == "GET") Apis.ApiGetClosedCustomers(ctx);
                else if (path == "/api/debits" && ctx.Request.HttpMethod == "GET") Apis.ApiGetDebits(ctx);
                else if (path == "/api/debits" && ctx.Request.HttpMethod == "POST") Apis.ApiAddDebit(ctx);
                else if (path == "/api/recoveries" && ctx.Request.HttpMethod == "GET") Apis.ApiGetRecoveries(ctx);
                else if (path == "/api/recoveries" && ctx.Request.HttpMethod == "POST") Apis.ApiAddRecovery(ctx);
                else if (path == "/api/ledger") Apis.ApiGetLedger(ctx);
                else if (path == "/api/admin/system-info") Apis.ApiAdminSystemInfo(ctx);
                else if (path == "/api/admin/change-password" && ctx.Request.HttpMethod == "POST") Apis.ApiAdminChangePassword(ctx);
                else if (path == "/api/admin/clear-transactions" && ctx.Request.HttpMethod == "POST") Apis.ApiAdminClearTransactions(ctx);
                else if (path == "/api/admin/reset-all" && ctx.Request.HttpMethod == "POST") Apis.ApiAdminResetAll(ctx);
                else Serve404(ctx);
            }
            catch (Exception ex) { Serve500(ctx, ex.ToString()); }
            finally { try { ctx.Response.OutputStream.Close(); } catch { } }
        }

        public static void ServeHtml(HttpListenerContext ctx, string html)
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            byte[] buf = Encoding.UTF8.GetBytes(html);
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        public static void ServeJs(HttpListenerContext ctx, string js)
        {
            ctx.Response.ContentType = "application/javascript; charset=utf-8";
            byte[] buf = Encoding.UTF8.GetBytes(js);
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        public static void SendJson(HttpListenerContext ctx, object data)
        {
            string j = json.Serialize(data);
            ctx.Response.ContentType = "application/json; charset=utf-8";
            byte[] buf = Encoding.UTF8.GetBytes(j);
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        static void Serve404(HttpListenerContext ctx) { ctx.Response.StatusCode = 404; ServeHtml(ctx, "<h1>404 Not Found</h1>"); }
        static void Serve500(HttpListenerContext ctx, string msg) { ctx.Response.StatusCode = 500; ServeHtml(ctx, "<h1>Server Error</h1><p>" + msg + "</p>"); }
    }
}