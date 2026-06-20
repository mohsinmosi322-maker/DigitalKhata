using System;
using System.IO;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Khatify
{
    class Apis
    {
        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static void ApiLogin(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, string>>(body);
                
                if (!data.ContainsKey("username") || !data.ContainsKey("password") || 
                    string.IsNullOrEmpty(data["username"]) || string.IsNullOrEmpty(data["password"]))
                {
                    Program.SendJson(ctx, new { success = false, message = "Username and password are required" });
                    return;
                }

                string hashedPassword = HashPassword(data["password"]);
                
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT Role FROM Users WHERE Username=@u AND Password=@p", conn);
                    cmd.Parameters.AddWithValue("@u", data["username"]);
                    cmd.Parameters.AddWithValue("@p", hashedPassword);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read()) 
                    {
                        string role = reader["Role"] != DBNull.Value ? reader["Role"].ToString() : "Operator";
                        Program.SendJson(ctx, new { success = true, role = role });
                    }
                    else 
                    {
                        Program.SendJson(ctx, new { success = false, message = "Invalid credentials" });
                    }
                }
            }
            catch (Exception ex)
            {
                Program.SendJson(ctx, new { success = false, message = "Login error: " + ex.Message });
            }
        }

        public static void ApiDashboard(System.Net.HttpListenerContext ctx)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    int customers = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Customers WHERE Status = 1", conn).ExecuteScalar());
                    decimal totalDebit = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(d.Amount),0) FROM Debits d INNER JOIN Customers c ON d.CustomerID=c.CustomerID WHERE c.Status=1", conn).ExecuteScalar());
                    decimal totalRec = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(r.AmountReceived),0) FROM Recoveries r INNER JOIN Customers c ON r.CustomerID=c.CustomerID WHERE c.Status=1", conn).ExecuteScalar());
                    decimal outstanding = Convert.ToDecimal(new SqlCommand("SELECT ISNULL(SUM(CurrentBalance),0) FROM Customers WHERE Status = 1", conn).ExecuteScalar());
                    Program.SendJson(ctx, new { customers = customers, totalDebit = totalDebit, totalRecovery = totalRec, outstanding = outstanding });
                }
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.ToString() }); }
        }

        public static void ApiGetCustomers(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var list = new List<object>();
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM Customers WHERE Status = 1 ORDER BY CustomerID DESC", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new {
                                CustomerID = reader["CustomerID"],
                                CustomerCode = reader["CustomerCode"] != DBNull.Value ? reader["CustomerCode"].ToString() : "",
                                CustomerName = reader["CustomerName"] != DBNull.Value ? reader["CustomerName"].ToString() : "",
                                Mobile = reader["Mobile"] != DBNull.Value ? reader["Mobile"].ToString() : "",
                                City = reader["City"] != DBNull.Value ? reader["City"].ToString() : "",
                                CurrentBalance = reader["CurrentBalance"] != DBNull.Value ? Convert.ToDecimal(reader["CurrentBalance"]) : 0m
                            });
                        }
                    }
                }
                Program.SendJson(ctx, list);
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.Message }); }
        }

        public static void ApiGetClosedCustomers(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var list = new List<object>();
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM Customers WHERE Status = 0 ORDER BY CustomerID DESC", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new {
                                CustomerID = reader["CustomerID"],
                                CustomerCode = reader["CustomerCode"] != DBNull.Value ? reader["CustomerCode"].ToString() : "",
                                CustomerName = reader["CustomerName"] != DBNull.Value ? reader["CustomerName"].ToString() : "",
                                Mobile = reader["Mobile"] != DBNull.Value ? reader["Mobile"].ToString() : "",
                                City = reader["City"] != DBNull.Value ? reader["City"].ToString() : "",
                                CurrentBalance = reader["CurrentBalance"] != DBNull.Value ? Convert.ToDecimal(reader["CurrentBalance"]) : 0m
                            });
                        }
                    }
                }
                Program.SendJson(ctx, list);
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.Message }); }
        }

        public static void ApiCloseCustomer(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                if (!data.ContainsKey("CustomerID"))
                {
                    Program.SendJson(ctx, new { success = false, message = "CustomerID is required" });
                    return;
                }
                
                int id = Convert.ToInt32(data["CustomerID"]);
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    new SqlCommand("UPDATE Customers SET Status = 0 WHERE CustomerID = @id", conn).ExecuteNonQuery();
                }
                Program.SendJson(ctx, new { success = true });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiReopenCustomer(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                if (!data.ContainsKey("CustomerID"))
                {
                    Program.SendJson(ctx, new { success = false, message = "CustomerID is required" });
                    return;
                }
                
                int id = Convert.ToInt32(data["CustomerID"]);
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    new SqlCommand("UPDATE Customers SET Status = 1 WHERE CustomerID = @id", conn).ExecuteNonQuery();
                }
                Program.SendJson(ctx, new { success = true });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiAddCustomer(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                // Server-side validation
                if (!data.ContainsKey("CustomerCode") || string.IsNullOrEmpty(data["CustomerCode"].ToString()))
                {
                    Program.SendJson(ctx, new { success = false, message = "Customer Code is required" });
                    return;
                }
                if (!data.ContainsKey("CustomerName") || string.IsNullOrEmpty(data["CustomerName"].ToString()))
                {
                    Program.SendJson(ctx, new { success = false, message = "Customer Name is required" });
                    return;
                }
                
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    
                    // Check for duplicate CustomerCode
                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE CustomerCode = @code", conn);
                    checkCmd.Parameters.AddWithValue("@code", data["CustomerCode"].ToString());
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        Program.SendJson(ctx, new { success = false, message = "Customer Code already exists" });
                        return;
                    }
                    
                    SqlCommand cmd = new SqlCommand("INSERT INTO Customers (CustomerCode, CustomerName, Mobile, City, OpeningBalance, CurrentBalance, Status) VALUES (@code, @name, @mob, @city, @ob, @ob, 1)", conn);
                    cmd.Parameters.AddWithValue("@code", data["CustomerCode"].ToString());
                    cmd.Parameters.AddWithValue("@name", data["CustomerName"].ToString());
                    cmd.Parameters.AddWithValue("@mob", data.ContainsKey("Mobile") && data["Mobile"] != null ? data["Mobile"].ToString() : "");
                    cmd.Parameters.AddWithValue("@city", data.ContainsKey("City") && data["City"] != null ? data["City"].ToString() : "");
                    cmd.Parameters.AddWithValue("@ob", data.ContainsKey("OpeningBalance") && data["OpeningBalance"] != null ? Convert.ToDecimal(data["OpeningBalance"]) : 0m);
                    cmd.ExecuteNonQuery();
                }
                Program.SendJson(ctx, new { success = true });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        // NEW: Delete Customer API
        public static void ApiDeleteCustomer(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                if (!data.ContainsKey("CustomerID"))
                {
                    Program.SendJson(ctx, new { success = false, message = "CustomerID is required" });
                    return;
                }
                
                int id = Convert.ToInt32(data["CustomerID"]);
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        // First check if customer has any transactions
                        SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Debits WHERE CustomerID = @id", conn, trans);
                        checkCmd.Parameters.AddWithValue("@id", id);
                        int debitCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                        
                        checkCmd = new SqlCommand("SELECT COUNT(*) FROM Recoveries WHERE CustomerID = @id", conn, trans);
                        int recoveryCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                        
                        if (debitCount > 0 || recoveryCount > 0)
                        {
                            Program.SendJson(ctx, new { success = false, message = "Cannot delete customer with existing transactions. Close the khata instead." });
                            trans.Rollback();
                            return;
                        }
                        
                        // Delete from CustomerLedger if any entries exist
                        new SqlCommand("DELETE FROM CustomerLedger WHERE CustomerID = @id", conn, trans).ExecuteNonQuery();
                        // Delete the customer
                        new SqlCommand("DELETE FROM Customers WHERE CustomerID = @id", conn, trans).ExecuteNonQuery();
                        
                        trans.Commit();
                        Program.SendJson(ctx, new { success = true });
                    }
                    catch (Exception ex) { 
                        trans.Rollback(); 
                        throw ex; 
                    }
                }
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiGetDebits(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var list = new List<object>();
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT d.*, c.CustomerName FROM Debits d INNER JOIN Customers c ON d.CustomerID = c.CustomerID WHERE c.Status = 1 ORDER BY d.DebitID DESC", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new {
                                DebitID = reader["DebitID"],
                                CustomerID = reader["CustomerID"],
                                CustomerName = reader["CustomerName"] != DBNull.Value ? reader["CustomerName"].ToString() : "",
                                DebitDate = reader["DebitDate"] != DBNull.Value ? Convert.ToDateTime(reader["DebitDate"]).ToString("yyyy-MM-dd") : "",
                                InvoiceNumber = reader["InvoiceNumber"] != DBNull.Value ? reader["InvoiceNumber"].ToString() : "",
                                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0m,
                                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : ""
                            });
                        }
                    }
                }
                Program.SendJson(ctx, list);
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.Message }); }
        }

        public static void ApiAddDebit(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                if (!data.ContainsKey("CustomerID") || !data.ContainsKey("Amount"))
                {
                    Program.SendJson(ctx, new { success = false, message = "CustomerID and Amount are required" });
                    return;
                }
                
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        int customerID = Convert.ToInt32(data["CustomerID"]);
                        decimal amount = Convert.ToDecimal(data["Amount"]);
                        string dateStr = data.ContainsKey("DebitDate") && data["DebitDate"] != null ? data["DebitDate"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                        DateTime debitDate;
                        if (!DateTime.TryParse(dateStr, out debitDate))
                        {
                            debitDate = DateTime.Now;
                        }

                        SqlCommand cmd = new SqlCommand("INSERT INTO Debits (CustomerID, DebitDate, InvoiceNumber, Amount, Description) OUTPUT INSERTED.DebitID VALUES (@cid, @date, @inv, @amt, @desc)", conn, trans);
                        cmd.Parameters.AddWithValue("@cid", customerID);
                        cmd.Parameters.AddWithValue("@date", debitDate);
                        cmd.Parameters.AddWithValue("@inv", data.ContainsKey("InvoiceNumber") && data["InvoiceNumber"] != null ? data["InvoiceNumber"].ToString() : "");
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@desc", data.ContainsKey("Description") && data["Description"] != null ? data["Description"].ToString() : "");
                        int debitID = (int)cmd.ExecuteScalar();
                        
                        var updCmd = new SqlCommand("UPDATE Customers SET CurrentBalance = CurrentBalance + @amt WHERE CustomerID = @cid", conn, trans);
                        updCmd.Parameters.AddWithValue("@amt", amount); 
                        updCmd.Parameters.AddWithValue("@cid", customerID);
                        updCmd.ExecuteNonQuery();
                        
                        decimal newBalance = (decimal)new SqlCommand("SELECT CurrentBalance FROM Customers WHERE CustomerID = @cid", conn, trans) { Parameters = { new SqlParameter("@cid", customerID) } }.ExecuteScalar();
                        
                        var ledCmd = new SqlCommand("INSERT INTO CustomerLedger (CustomerID, TransactionDate, VoucherType, ReferenceID, Description, DebitAmount, RunningBalance) VALUES (@cid, @date, 'Debit', @refid, @desc, @amt, @bal)", conn, trans);
                        ledCmd.Parameters.AddWithValue("@cid", customerID); 
                        ledCmd.Parameters.AddWithValue("@date", debitDate);
                        ledCmd.Parameters.AddWithValue("@refid", debitID); 
                        ledCmd.Parameters.AddWithValue("@desc", data.ContainsKey("Description") && data["Description"] != null ? data["Description"].ToString() : "");
                        ledCmd.Parameters.AddWithValue("@amt", amount); 
                        ledCmd.Parameters.AddWithValue("@bal", newBalance);
                        ledCmd.ExecuteNonQuery();
                        
                        trans.Commit();
                        Program.SendJson(ctx, new { success = true });
                    }
                    catch { 
                        trans.Rollback(); 
                        throw; 
                    }
                    finally { conn.Close(); }
                }
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiGetRecoveries(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var list = new List<object>();
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT r.*, c.CustomerName FROM Recoveries r INNER JOIN Customers c ON r.CustomerID = c.CustomerID WHERE c.Status = 1 ORDER BY r.RecoveryID DESC", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new {
                                RecoveryID = reader["RecoveryID"],
                                CustomerID = reader["CustomerID"],
                                CustomerName = reader["CustomerName"] != DBNull.Value ? reader["CustomerName"].ToString() : "",
                                RecoveryDate = reader["RecoveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["RecoveryDate"]).ToString("yyyy-MM-dd") : "",
                                AmountReceived = reader["AmountReceived"] != DBNull.Value ? Convert.ToDecimal(reader["AmountReceived"]) : 0m,
                                PaymentMethod = reader["PaymentMethod"] != DBNull.Value ? reader["PaymentMethod"].ToString() : "",
                                ReferenceNumber = reader["ReferenceNumber"] != DBNull.Value ? reader["ReferenceNumber"].ToString() : ""
                            });
                        }
                    }
                }
                Program.SendJson(ctx, list);
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.Message }); }
        }

        public static void ApiAddRecovery(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                if (!data.ContainsKey("CustomerID") || !data.ContainsKey("AmountReceived"))
                {
                    Program.SendJson(ctx, new { success = false, message = "CustomerID and AmountReceived are required" });
                    return;
                }
                
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        int customerID = Convert.ToInt32(data["CustomerID"]);
                        decimal amount = Convert.ToDecimal(data["AmountReceived"]);
                        string dateStr = data.ContainsKey("RecoveryDate") && data["RecoveryDate"] != null ? data["RecoveryDate"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                        DateTime recoveryDate;
                        if (!DateTime.TryParse(dateStr, out recoveryDate))
                        {
                            recoveryDate = DateTime.Now;
                        }

                        SqlCommand cmd = new SqlCommand("INSERT INTO Recoveries (CustomerID, RecoveryDate, AmountReceived, PaymentMethod, ReferenceNumber, Remarks) OUTPUT INSERTED.RecoveryID VALUES (@cid, @date, @amt, @pm, @ref, @rem)", conn, trans);
                        cmd.Parameters.AddWithValue("@cid", customerID); 
                        cmd.Parameters.AddWithValue("@date", recoveryDate);
                        cmd.Parameters.AddWithValue("@amt", amount); 
                        cmd.Parameters.AddWithValue("@pm", data.ContainsKey("PaymentMethod") && data["PaymentMethod"] != null ? data["PaymentMethod"].ToString() : "Cash");
                        cmd.Parameters.AddWithValue("@ref", data.ContainsKey("ReferenceNumber") && data["ReferenceNumber"] != null ? data["ReferenceNumber"].ToString() : "");
                        cmd.Parameters.AddWithValue("@rem", data.ContainsKey("Remarks") && data["Remarks"] != null ? data["Remarks"].ToString() : "");
                        int recoveryID = (int)cmd.ExecuteScalar();
                        
                        var updCmd = new SqlCommand("UPDATE Customers SET CurrentBalance = CurrentBalance - @amt WHERE CustomerID = @cid", conn, trans);
                        updCmd.Parameters.AddWithValue("@amt", amount); 
                        updCmd.Parameters.AddWithValue("@cid", customerID);
                        updCmd.ExecuteNonQuery();
                        
                        decimal newBalance = (decimal)new SqlCommand("SELECT CurrentBalance FROM Customers WHERE CustomerID = @cid", conn, trans) { Parameters = { new SqlParameter("@cid", customerID) } }.ExecuteScalar();
                        
                        var ledCmd = new SqlCommand("INSERT INTO CustomerLedger (CustomerID, TransactionDate, VoucherType, ReferenceID, Description, CreditAmount, RunningBalance) VALUES (@cid, @date, 'Recovery', @refid, @pm, @amt, @bal)", conn, trans);
                        ledCmd.Parameters.AddWithValue("@cid", customerID); 
                        ledCmd.Parameters.AddWithValue("@date", recoveryDate);
                        ledCmd.Parameters.AddWithValue("@refid", recoveryID); 
                        ledCmd.Parameters.AddWithValue("@pm", data.ContainsKey("PaymentMethod") && data["PaymentMethod"] != null ? data["PaymentMethod"].ToString() : "Cash");
                        ledCmd.Parameters.AddWithValue("@amt", amount); 
                        ledCmd.Parameters.AddWithValue("@bal", newBalance);
                        ledCmd.ExecuteNonQuery();
                        
                        trans.Commit();
                        Program.SendJson(ctx, new { success = true });
                    }
                    catch { 
                        trans.Rollback(); 
                        throw; 
                    }
                    finally { conn.Close(); }
                }
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiGetLedger(System.Net.HttpListenerContext ctx)
        {
            try
            {
                string customerIdStr = ctx.Request.QueryString["customerID"];
                if (string.IsNullOrEmpty(customerIdStr))
                {
                    Program.SendJson(ctx, new { error = "customerID is required" });
                    return;
                }
                
                int customerID = Convert.ToInt32(customerIdStr);
                var list = new List<object>();
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM CustomerLedger WHERE CustomerID = @cid ORDER BY TransactionDate DESC, LedgerID DESC", conn);
                    cmd.Parameters.AddWithValue("@cid", customerID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new {
                                LedgerID = reader["LedgerID"],
                                TransactionDate = reader["TransactionDate"] != DBNull.Value ? Convert.ToDateTime(reader["TransactionDate"]).ToString("yyyy-MM-dd HH:mm") : "",
                                VoucherType = reader["VoucherType"] != DBNull.Value ? reader["VoucherType"].ToString() : "",
                                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                                DebitAmount = reader["DebitAmount"] != DBNull.Value ? Convert.ToDecimal(reader["DebitAmount"]) : 0m,
                                CreditAmount = reader["CreditAmount"] != DBNull.Value ? Convert.ToDecimal(reader["CreditAmount"]) : 0m,
                                RunningBalance = reader["RunningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["RunningBalance"]) : 0m
                            });
                        }
                    }
                }
                Program.SendJson(ctx, list);
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.Message }); }
        }

        // ============ ADMIN APIs ============
        public static void ApiAdminSystemInfo(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var users = new List<object>();
                int totalUsers = 0;
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    totalUsers = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Users", conn).ExecuteScalar());
                    SqlCommand cmd = new SqlCommand("SELECT UserID, Username, Role, CreatedDate FROM Users ORDER BY UserID", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new {
                                UserID = reader["UserID"],
                                Username = reader["Username"].ToString(),
                                Role = reader["Role"].ToString(),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]).ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
                Program.SendJson(ctx, new { 
                    totalUsers = totalUsers, 
                    users = users, 
                    serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    dbStatus = "Connected"
                });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { error = ex.Message }); }
        }

        public static void ApiAdminChangePassword(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                var data = Program.json.Deserialize<Dictionary<string, object>>(body);
                
                if (!data.ContainsKey("UserID") || !data.ContainsKey("CurrentPassword") || !data.ContainsKey("NewPassword"))
                {
                    Program.SendJson(ctx, new { success = false, message = "UserID, CurrentPassword and NewPassword are required" });
                    return;
                }
                
                int userId = Convert.ToInt32(data["UserID"]);
                string currentPwd = data["CurrentPassword"].ToString();
                string newPwd = data["NewPassword"].ToString();
                string newUsername = data.ContainsKey("NewUsername") && !string.IsNullOrEmpty(data["NewUsername"].ToString()) ? data["NewUsername"].ToString() : null;

                // Hash the passwords for comparison
                string hashedCurrentPwd = HashPassword(currentPwd);
                string hashedNewPwd = HashPassword(newPwd);

                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    var verifyCmd = new SqlCommand("SELECT Password FROM Users WHERE UserID = @id", conn);
                    verifyCmd.Parameters.AddWithValue("@id", userId);
                    object result = verifyCmd.ExecuteScalar();
                    
                    if (result == null || result == DBNull.Value)
                    {
                        Program.SendJson(ctx, new { success = false, message = "User not found!" });
                        return;
                    }
                    
                    string storedPwd = result.ToString();
                    if (storedPwd != hashedCurrentPwd)
                    {
                        Program.SendJson(ctx, new { success = false, message = "Current password is incorrect!" });
                        return;
                    }

                    if (newUsername != null)
                    {
                        var updCmd = new SqlCommand("UPDATE Users SET Password = @newpwd, Username = @newuser WHERE UserID = @id", conn);
                        updCmd.Parameters.AddWithValue("@newpwd", hashedNewPwd);
                        updCmd.Parameters.AddWithValue("@newuser", newUsername);
                        updCmd.Parameters.AddWithValue("@id", userId);
                        updCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        var updCmd = new SqlCommand("UPDATE Users SET Password = @newpwd WHERE UserID = @id", conn);
                        updCmd.Parameters.AddWithValue("@newpwd", hashedNewPwd);
                        updCmd.Parameters.AddWithValue("@id", userId);
                        updCmd.ExecuteNonQuery();
                    }
                }
                Program.SendJson(ctx, new { success = true });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiAdminClearTransactions(System.Net.HttpListenerContext ctx)
        {
             try
            {
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        new SqlCommand("DELETE FROM CustomerLedger", conn, trans).ExecuteNonQuery();
                        new SqlCommand("DELETE FROM Debits", conn, trans).ExecuteNonQuery();
                        new SqlCommand("DELETE FROM Recoveries", conn, trans).ExecuteNonQuery();
                        new SqlCommand("UPDATE Customers SET CurrentBalance = OpeningBalance", conn, trans).ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch { trans.Rollback(); throw; }
                    finally { conn.Close(); }
                }
                Program.SendJson(ctx, new { success = true });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }

        public static void ApiAdminResetAll(System.Net.HttpListenerContext ctx)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    SqlTransaction trans = conn.BeginTransaction();
                    try
                    {
                        new SqlCommand("DELETE FROM CustomerLedger", conn, trans).ExecuteNonQuery();
                        new SqlCommand("DELETE FROM Debits", conn, trans).ExecuteNonQuery();
                        new SqlCommand("DELETE FROM Recoveries", conn, trans).ExecuteNonQuery();
                        new SqlCommand("DELETE FROM Customers", conn, trans).ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch { trans.Rollback(); throw; }
                    finally { conn.Close(); }
                }
                Program.SendJson(ctx, new { success = true });
            }
            catch (Exception ex) { Program.SendJson(ctx, new { success = false, message = ex.Message }); }
        }
    }
}