using System;
using System.IO;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Khatify
{
    class Apis
    {
        public static void ApiLogin(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, string>>(body);
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Role FROM Users WHERE Username=@u AND Password=@p", conn);
                cmd.Parameters.AddWithValue("@u", data["username"]);
                cmd.Parameters.AddWithValue("@p", data["password"]);
                var reader = cmd.ExecuteReader();
                if (reader.Read()) Program.SendJson(ctx, new { success = true, role = reader["Role"].ToString() });
                else Program.SendJson(ctx, new { success = false, message = "Invalid credentials" });
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
                            CustomerCode = reader["CustomerCode"].ToString(),
                            CustomerName = reader["CustomerName"].ToString(),
                            Mobile = reader["Mobile"].ToString(),
                            City = reader["City"].ToString(),
                            CurrentBalance = Convert.ToDecimal(reader["CurrentBalance"])
                        });
                    }
                }
            }
            Program.SendJson(ctx, list);
        }

        public static void ApiGetClosedCustomers(System.Net.HttpListenerContext ctx)
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
                            CustomerCode = reader["CustomerCode"].ToString(),
                            CustomerName = reader["CustomerName"].ToString(),
                            Mobile = reader["Mobile"].ToString(),
                            City = reader["City"].ToString(),
                            CurrentBalance = Convert.ToDecimal(reader["CurrentBalance"])
                        });
                    }
                }
            }
            Program.SendJson(ctx, list);
        }

        public static void ApiCloseCustomer(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, object>>(body);
            int id = Convert.ToInt32(data["CustomerID"]);
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                new SqlCommand("UPDATE Customers SET Status = 0 WHERE CustomerID = @id", conn).ExecuteNonQuery();
            }
            Program.SendJson(ctx, new { success = true });
        }

        public static void ApiReopenCustomer(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, object>>(body);
            int id = Convert.ToInt32(data["CustomerID"]);
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                new SqlCommand("UPDATE Customers SET Status = 1 WHERE CustomerID = @id", conn).ExecuteNonQuery();
            }
            Program.SendJson(ctx, new { success = true });
        }

        public static void ApiAddCustomer(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, object>>(body);
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("INSERT INTO Customers (CustomerCode, CustomerName, Mobile, City, OpeningBalance, CurrentBalance, Status) VALUES (@code, @name, @mob, @city, @ob, @ob, 1)", conn);
                cmd.Parameters.AddWithValue("@code", data["CustomerCode"].ToString());
                cmd.Parameters.AddWithValue("@name", data["CustomerName"].ToString());
                cmd.Parameters.AddWithValue("@mob", data.ContainsKey("Mobile") ? data["Mobile"].ToString() : "");
                cmd.Parameters.AddWithValue("@city", data.ContainsKey("City") ? data["City"].ToString() : "");
                cmd.Parameters.AddWithValue("@ob", Convert.ToDecimal(data.ContainsKey("OpeningBalance") ? data["OpeningBalance"] : 0));
                cmd.ExecuteNonQuery();
            }
            Program.SendJson(ctx, new { success = true });
        }

        public static void ApiGetDebits(System.Net.HttpListenerContext ctx)
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
                            CustomerName = reader["CustomerName"].ToString(),
                            DebitDate = Convert.ToDateTime(reader["DebitDate"]).ToString("yyyy-MM-dd"),
                            InvoiceNumber = reader["InvoiceNumber"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"]),
                            Description = reader["Description"].ToString()
                        });
                    }
                }
            }
            Program.SendJson(ctx, list);
        }

        public static void ApiAddDebit(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, object>>(body);
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    int customerID = Convert.ToInt32(data["CustomerID"]);
                    decimal amount = Convert.ToDecimal(data["Amount"]);
                    string dateStr = data.ContainsKey("DebitDate") && data["DebitDate"] != null ? data["DebitDate"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                    DateTime debitDate = DateTime.Parse(dateStr);

                    SqlCommand cmd = new SqlCommand("INSERT INTO Debits (CustomerID, DebitDate, InvoiceNumber, Amount, Description) OUTPUT INSERTED.DebitID VALUES (@cid, @date, @inv, @amt, @desc)", conn, trans);
                    cmd.Parameters.AddWithValue("@cid", customerID);
                    cmd.Parameters.AddWithValue("@date", debitDate);
                    cmd.Parameters.AddWithValue("@inv", data.ContainsKey("InvoiceNumber") ? data["InvoiceNumber"].ToString() : "");
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@desc", data.ContainsKey("Description") ? data["Description"].ToString() : "");
                    int debitID = (int)cmd.ExecuteScalar();
                    
                    var updCmd = new SqlCommand("UPDATE Customers SET CurrentBalance = CurrentBalance + @amt WHERE CustomerID = @cid", conn, trans);
                    updCmd.Parameters.AddWithValue("@amt", amount); 
                    updCmd.Parameters.AddWithValue("@cid", customerID);
                    updCmd.ExecuteNonQuery();
                    
                    decimal newBalance = (decimal)new SqlCommand("SELECT CurrentBalance FROM Customers WHERE CustomerID = @cid", conn, trans) { Parameters = { { "@cid", customerID } } }.ExecuteScalar();
                    
                    var ledCmd = new SqlCommand("INSERT INTO CustomerLedger (CustomerID, TransactionDate, VoucherType, ReferenceID, Description, DebitAmount, RunningBalance) VALUES (@cid, @date, 'Debit', @refid, @desc, @amt, @bal)", conn, trans);
                    ledCmd.Parameters.AddWithValue("@cid", customerID); 
                    ledCmd.Parameters.AddWithValue("@date", debitDate);
                    ledCmd.Parameters.AddWithValue("@refid", debitID); 
                    ledCmd.Parameters.AddWithValue("@desc", data.ContainsKey("Description") ? data["Description"].ToString() : "");
                    ledCmd.Parameters.AddWithValue("@amt", amount); 
                    ledCmd.Parameters.AddWithValue("@bal", newBalance);
                    ledCmd.ExecuteNonQuery();
                    
                    trans.Commit();
                    Program.SendJson(ctx, new { success = true });
                }
                catch { trans.Rollback(); throw; }
                finally { conn.Close(); }
            }
        }

        public static void ApiGetRecoveries(System.Net.HttpListenerContext ctx)
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
                            CustomerName = reader["CustomerName"].ToString(),
                            RecoveryDate = Convert.ToDateTime(reader["RecoveryDate"]).ToString("yyyy-MM-dd"),
                            AmountReceived = Convert.ToDecimal(reader["AmountReceived"]),
                            PaymentMethod = reader["PaymentMethod"].ToString(),
                            ReferenceNumber = reader["ReferenceNumber"].ToString()
                        });
                    }
                }
            }
            Program.SendJson(ctx, list);
        }

        public static void ApiAddRecovery(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, object>>(body);
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    int customerID = Convert.ToInt32(data["CustomerID"]);
                    decimal amount = Convert.ToDecimal(data["AmountReceived"]);
                    string dateStr = data.ContainsKey("RecoveryDate") && data["RecoveryDate"] != null ? data["RecoveryDate"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                    DateTime recoveryDate = DateTime.Parse(dateStr);

                    SqlCommand cmd = new SqlCommand("INSERT INTO Recoveries (CustomerID, RecoveryDate, AmountReceived, PaymentMethod, ReferenceNumber, Remarks) OUTPUT INSERTED.RecoveryID VALUES (@cid, @date, @amt, @pm, @ref, @rem)", conn, trans);
                    cmd.Parameters.AddWithValue("@cid", customerID); 
                    cmd.Parameters.AddWithValue("@date", recoveryDate);
                    cmd.Parameters.AddWithValue("@amt", amount); 
                    cmd.Parameters.AddWithValue("@pm", data.ContainsKey("PaymentMethod") ? data["PaymentMethod"].ToString() : "Cash");
                    cmd.Parameters.AddWithValue("@ref", data.ContainsKey("ReferenceNumber") ? data["ReferenceNumber"].ToString() : "");
                    cmd.Parameters.AddWithValue("@rem", data.ContainsKey("Remarks") ? data["Remarks"].ToString() : "");
                    int recoveryID = (int)cmd.ExecuteScalar();
                    
                    var updCmd = new SqlCommand("UPDATE Customers SET CurrentBalance = CurrentBalance - @amt WHERE CustomerID = @cid", conn, trans);
                    updCmd.Parameters.AddWithValue("@amt", amount); 
                    updCmd.Parameters.AddWithValue("@cid", customerID);
                    updCmd.ExecuteNonQuery();
                    
                    decimal newBalance = (decimal)new SqlCommand("SELECT CurrentBalance FROM Customers WHERE CustomerID = @cid", conn, trans) { Parameters = { { "@cid", customerID } } }.ExecuteScalar();
                    
                    var ledCmd = new SqlCommand("INSERT INTO CustomerLedger (CustomerID, TransactionDate, VoucherType, ReferenceID, Description, CreditAmount, RunningBalance) VALUES (@cid, @date, 'Recovery', @refid, @pm, @amt, @bal)", conn, trans);
                    ledCmd.Parameters.AddWithValue("@cid", customerID); 
                    ledCmd.Parameters.AddWithValue("@date", recoveryDate);
                    ledCmd.Parameters.AddWithValue("@refid", recoveryID); 
                    ledCmd.Parameters.AddWithValue("@pm", data.ContainsKey("PaymentMethod") ? data["PaymentMethod"].ToString() : "Cash");
                    ledCmd.Parameters.AddWithValue("@amt", amount); 
                    ledCmd.Parameters.AddWithValue("@bal", newBalance);
                    ledCmd.ExecuteNonQuery();
                    
                    trans.Commit();
                    Program.SendJson(ctx, new { success = true });
                }
                catch { trans.Rollback(); throw; }
                finally { conn.Close(); }
            }
        }

        public static void ApiGetLedger(System.Net.HttpListenerContext ctx)
        {
            int customerID = Convert.ToInt32(ctx.Request.QueryString["customerID"]);
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
                            TransactionDate = Convert.ToDateTime(reader["TransactionDate"]).ToString("yyyy-MM-dd HH:mm"),
                            VoucherType = reader["VoucherType"].ToString(),
                            Description = reader["Description"].ToString(),
                            DebitAmount = Convert.ToDecimal(reader["DebitAmount"]),
                            CreditAmount = Convert.ToDecimal(reader["CreditAmount"]),
                            RunningBalance = Convert.ToDecimal(reader["RunningBalance"])
                        });
                    }
                }
            }
            Program.SendJson(ctx, list);
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
                int userId = Convert.ToInt32(data["UserID"]);
                string currentPwd = data["CurrentPassword"].ToString();
                string newPwd = data["NewPassword"].ToString();
                string newUsername = data.ContainsKey("NewUsername") && !string.IsNullOrEmpty(data["NewUsername"].ToString()) ? data["NewUsername"].ToString() : null;

                using (SqlConnection conn = new SqlConnection(Program.connStr))
                {
                    conn.Open();
                    var verifyCmd = new SqlCommand("SELECT Password FROM Users WHERE UserID = @id", conn);
                    verifyCmd.Parameters.AddWithValue("@id", userId);
                    string storedPwd = verifyCmd.ExecuteScalar().ToString();
                    if (storedPwd != currentPwd)
                    {
                        Program.SendJson(ctx, new { success = false, message = "Current password is incorrect!" });
                        return;
                    }

                    if (newUsername != null)
                    {
                         var updCmd = new SqlCommand("UPDATE Users SET Password = @newpwd, Username = @newuser WHERE UserID = @id", conn);
                        updCmd.Parameters.AddWithValue("@newpwd", newPwd);
                        updCmd.Parameters.AddWithValue("@newuser", newUsername);
                        updCmd.Parameters.AddWithValue("@id", userId);
                        updCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        var updCmd = new SqlCommand("UPDATE Users SET Password = @newpwd WHERE UserID = @id", conn);
                        updCmd.Parameters.AddWithValue("@newpwd", newPwd);
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