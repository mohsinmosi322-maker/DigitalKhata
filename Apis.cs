using System;
using System.IO;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace DigitalKhata
{
    class Apis
    {
        // ==================== LOGIN API ====================
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
                if (reader.Read()) 
                    Program.SendJson(ctx, new { success = true, role = reader["Role"].ToString() });
                else 
                    Program.SendJson(ctx, new { success = false, message = "Invalid credentials" });
            }
        }

        // ==================== DASHBOARD API ====================
        public static void ApiDashboard(System.Net.HttpListenerContext ctx)
        {
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                int customers = (int)new SqlCommand("SELECT COUNT(*) FROM Customers", conn).ExecuteScalar();
                decimal totalDebit = (decimal)new SqlCommand("SELECT ISNULL(SUM(Amount),0) FROM Debits", conn).ExecuteScalar();
                decimal totalRec = (decimal)new SqlCommand("SELECT ISNULL(SUM(AmountReceived),0) FROM Recoveries", conn).ExecuteScalar();
                decimal outstanding = (decimal)new SqlCommand("SELECT ISNULL(SUM(CurrentBalance),0) FROM Customers", conn).ExecuteScalar();
                
                Program.SendJson(ctx, new { 
                    customers = customers,
                    totalDebit = totalDebit,
                    totalRecovery = totalRec,
                    outstanding = outstanding
                });
            }
        }

        // ==================== CUSTOMERS APIs ====================
        public static void ApiGetCustomers(System.Net.HttpListenerContext ctx)
        {
            var list = new List<object>();
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Customers ORDER BY CustomerID DESC", conn);
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

        public static void ApiAddCustomer(System.Net.HttpListenerContext ctx)
        {
            var body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            var data = Program.json.Deserialize<Dictionary<string, object>>(body);
            
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Customers (CustomerCode, CustomerName, Mobile, City, OpeningBalance, CurrentBalance, Status) VALUES (@code, @name, @mob, @city, @ob, @ob, 1)", conn);
                cmd.Parameters.AddWithValue("@code", data["CustomerCode"].ToString());
                cmd.Parameters.AddWithValue("@name", data["CustomerName"].ToString());
                cmd.Parameters.AddWithValue("@mob", data.ContainsKey("Mobile") ? data["Mobile"].ToString() : "");
                cmd.Parameters.AddWithValue("@city", data.ContainsKey("City") ? data["City"].ToString() : "");
                cmd.Parameters.AddWithValue("@ob", Convert.ToDecimal(data.ContainsKey("OpeningBalance") ? data["OpeningBalance"] : 0));
                cmd.ExecuteNonQuery();
            }
            Program.SendJson(ctx, new { success = true });
        }

        // ==================== DEBIT APIs ====================
        public static void ApiGetDebits(System.Net.HttpListenerContext ctx)
        {
            var list = new List<object>();
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT d.*, c.CustomerName 
                    FROM Debits d 
                    INNER JOIN Customers c ON d.CustomerID = c.CustomerID 
                    ORDER BY d.DebitID DESC", conn);
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
                    
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Debits (CustomerID, InvoiceNumber, Amount, Description) OUTPUT INSERTED.DebitID VALUES (@cid, @inv, @amt, @desc)", conn, trans);
                    cmd.Parameters.AddWithValue("@cid", customerID);
                    cmd.Parameters.AddWithValue("@inv", data["InvoiceNumber"].ToString());
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@desc", data["Description"].ToString());
                    int debitID = (int)cmd.ExecuteScalar();
                    
                    SqlCommand updCmd = new SqlCommand("UPDATE Customers SET CurrentBalance = CurrentBalance + @amt WHERE CustomerID = @cid", conn, trans);
                    updCmd.Parameters.AddWithValue("@amt", amount);
                    updCmd.Parameters.AddWithValue("@cid", customerID);
                    updCmd.ExecuteNonQuery();
                    
                    SqlCommand selCmd = new SqlCommand("SELECT CurrentBalance FROM Customers WHERE CustomerID = @cid", conn, trans);
                    selCmd.Parameters.AddWithValue("@cid", customerID);
                    decimal newBalance = (decimal)selCmd.ExecuteScalar();
                    
                    SqlCommand ledCmd = new SqlCommand(
                        "INSERT INTO CustomerLedger (CustomerID, TransactionDate, VoucherType, ReferenceID, Description, DebitAmount, RunningBalance) VALUES (@cid, GETDATE(), 'Debit', @refid, @desc, @amt, @bal)", 
                        conn, trans);
                    ledCmd.Parameters.AddWithValue("@cid", customerID);
                    ledCmd.Parameters.AddWithValue("@refid", debitID);
                    ledCmd.Parameters.AddWithValue("@desc", data["Description"].ToString());
                    ledCmd.Parameters.AddWithValue("@amt", amount);
                    ledCmd.Parameters.AddWithValue("@bal", newBalance);
                    ledCmd.ExecuteNonQuery();
                    
                    trans.Commit();
                    Program.SendJson(ctx, new { success = true });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        // ==================== RECOVERY APIs ====================
        public static void ApiGetRecoveries(System.Net.HttpListenerContext ctx)
        {
            var list = new List<object>();
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.*, c.CustomerName 
                    FROM Recoveries r 
                    INNER JOIN Customers c ON r.CustomerID = c.CustomerID 
                    ORDER BY r.RecoveryID DESC", conn);
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
                    
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Recoveries (CustomerID, AmountReceived, PaymentMethod, ReferenceNumber, Remarks) OUTPUT INSERTED.RecoveryID VALUES (@cid, @amt, @pm, @ref, @rem)", conn, trans);
                    cmd.Parameters.AddWithValue("@cid", customerID);
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@pm", data["PaymentMethod"].ToString());
                    cmd.Parameters.AddWithValue("@ref", data["ReferenceNumber"].ToString());
                    cmd.Parameters.AddWithValue("@rem", data["Remarks"].ToString());
                    int recoveryID = (int)cmd.ExecuteScalar();
                    
                    SqlCommand updCmd = new SqlCommand("UPDATE Customers SET CurrentBalance = CurrentBalance - @amt WHERE CustomerID = @cid", conn, trans);
                    updCmd.Parameters.AddWithValue("@amt", amount);
                    updCmd.Parameters.AddWithValue("@cid", customerID);
                    updCmd.ExecuteNonQuery();
                    
                    SqlCommand selCmd = new SqlCommand("SELECT CurrentBalance FROM Customers WHERE CustomerID = @cid", conn, trans);
                    selCmd.Parameters.AddWithValue("@cid", customerID);
                    decimal newBalance = (decimal)selCmd.ExecuteScalar();
                    
                    SqlCommand ledCmd = new SqlCommand(
                        "INSERT INTO CustomerLedger (CustomerID, TransactionDate, VoucherType, ReferenceID, Description, CreditAmount, RunningBalance) VALUES (@cid, GETDATE(), 'Recovery', @refid, @pm, @amt, @bal)", 
                        conn, trans);
                    ledCmd.Parameters.AddWithValue("@cid", customerID);
                    ledCmd.Parameters.AddWithValue("@refid", recoveryID);
                    ledCmd.Parameters.AddWithValue("@pm", data["PaymentMethod"].ToString());
                    ledCmd.Parameters.AddWithValue("@amt", amount);
                    ledCmd.Parameters.AddWithValue("@bal", newBalance);
                    ledCmd.ExecuteNonQuery();
                    
                    trans.Commit();
                    Program.SendJson(ctx, new { success = true });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        // ==================== LEDGER API ====================
        public static void ApiGetLedger(System.Net.HttpListenerContext ctx)
        {
            int customerID = Convert.ToInt32(ctx.Request.QueryString["customerID"]);
            var list = new List<object>();
            
            using (SqlConnection conn = new SqlConnection(Program.connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT * FROM CustomerLedger WHERE CustomerID = @cid ORDER BY TransactionDate DESC, LedgerID DESC", conn);
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
    }
}