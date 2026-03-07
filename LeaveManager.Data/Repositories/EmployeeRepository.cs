using LeaveManager.Data.Models;
using LeaveManager.Data.Storage;
using LeaveManager.Models;
using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Repositories
{
    public sealed class EmployeeRepository
    {
        private string ConnectionString =>
            $"Data Source={DbPaths.GetDbFilePath()}";

        // -----------------------------
        // GET ALL ACTIVE
        // -----------------------------
        public List<Employee> GetAllActive()
        {
            var list = new List<Employee>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id, sicil_no, full_name, role, manager_id, is_active
                FROM Employees
                WHERE is_active = 1
                ORDER BY full_name;
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        // -----------------------------
        // GET BY ID 
        // -----------------------------
        public Employee? GetById(int id)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id, sicil_no, full_name, role, manager_id, is_active
                FROM Employees
                WHERE id = @id;
            ";

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Map(reader);
            }

            return null;
        }

        // -----------------------------
        // ADD
        // -----------------------------
        public void Add(Employee employee)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {

                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;

                cmd.CommandText = @"
            INSERT INTO Employees
            (sicil_no, full_name, role, manager_id, is_active)
            VALUES
            (@sicil_no, @full_name, @role, @manager_id, 1);
        ";

                cmd.Parameters.AddWithValue("@sicil_no", employee.SicilNo);
                cmd.Parameters.AddWithValue("@full_name", employee.FullName);
                cmd.Parameters.AddWithValue("@role", (int)employee.Role);
                cmd.Parameters.AddWithValue("@manager_id",
                    employee.ManagerId.HasValue ? employee.ManagerId : DBNull.Value);

                cmd.ExecuteNonQuery();


                long employeeId;

                using (var idCmd = connection.CreateCommand())
                {
                    idCmd.Transaction = transaction;
                    idCmd.CommandText = "SELECT last_insert_rowid();";

                    object? result = idCmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        throw new InvalidOperationException("Employee ID alınamadı.");

                    employeeId = Convert.ToInt64(result);
                }


                using (var balanceCmd = connection.CreateCommand())
                {
                    balanceCmd.Transaction = transaction;

                    balanceCmd.CommandText = @"
                INSERT OR IGNORE INTO LeaveBalances
                (employee_id, year)
                VALUES (@empId, @year);
            ";

                    balanceCmd.Parameters.AddWithValue("@empId", employeeId);
                    balanceCmd.Parameters.AddWithValue("@year", DateTime.Now.Year);

                    balanceCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                transaction.Rollback();
                throw new InvalidOperationException("Bu sicil numarası zaten kayıtlı.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // -----------------------------
        // UPDATE
        // -----------------------------
        public void Update(Employee employee)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Employees
                SET full_name = @full_name,
                    role = @role,
                    manager_id = @manager_id
                WHERE id = @id;
            ";

            cmd.Parameters.AddWithValue("@id", employee.Id);
            cmd.Parameters.AddWithValue("@full_name", employee.FullName);
            cmd.Parameters.AddWithValue("@role", (int)employee.Role);
            cmd.Parameters.AddWithValue("@manager_id",
                employee.ManagerId.HasValue ? employee.ManagerId : DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // -----------------------------
        // SOFT DELETE
        // -----------------------------
        public void SoftDelete(int id)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Employees
                SET is_active = 0
                WHERE id = @id;
            ";

            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // -----------------------------
        // GET ASSISTANTS
        // -----------------------------
        public List<Employee> GetAssistants()
        {
            var list = new List<Employee>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id, sicil_no, full_name, role, manager_id, is_active
                FROM Employees
                WHERE role = @role AND is_active = 1
                ORDER BY full_name;
            ";

            cmd.Parameters.AddWithValue("@role", (int)EmployeeRole.Assistant);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        // -----------------------------
        // GET MANAGER FOR DATE
        // -----------------------------
        public int? GetManagerIdForDate(int employeeId, DateTime date)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT ManagerId
        FROM EmployeeManagerAssignments
        WHERE EmployeeId = @empId
        AND Year = @year
        AND Month = @month
        LIMIT 1;
    ";

            cmd.Parameters.AddWithValue("@empId", employeeId);
            cmd.Parameters.AddWithValue("@year", date.Year);
            cmd.Parameters.AddWithValue("@month", date.Month);

            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }

        // -----------------------------
        // GET ASSIGNMENTS
        // -----------------------------
        public List<EmployeeManagerAssignment> GetManagerAssignments(int employeeId)
        {
            var list = new List<EmployeeManagerAssignment>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT Id, EmployeeId, ManagerId, Year, Month
        FROM EmployeeManagerAssignments
        WHERE EmployeeId = @empId
        ORDER BY Year, Month;
    ";

            cmd.Parameters.AddWithValue("@empId", employeeId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new EmployeeManagerAssignment
                {
                    Id = reader.GetInt32(0),
                    EmployeeId = reader.GetInt32(1),
                    ManagerId = reader.GetInt32(2),
                    Year = reader.GetInt32(3),
                    Month = reader.GetInt32(4)
                });
            }

            return list;
        }


        public List<EmployeeManagerAssignment> GetAllManagerAssignments()
        {
            var list = new List<EmployeeManagerAssignment>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT Id, EmployeeId, ManagerId, Year, Month
        FROM EmployeeManagerAssignments;
    ";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new EmployeeManagerAssignment
                {
                    Id = reader.GetInt32(0),
                    EmployeeId = reader.GetInt32(1),
                    ManagerId = reader.GetInt32(2),
                    Year = reader.GetInt32(3),
                    Month = reader.GetInt32(4)
                });
            }

            return list;
        }
        // -----------------------------
        // ADD ASSIGNMENT
        // -----------------------------
        public void AddAssignment(EmployeeManagerAssignment assignment)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO EmployeeManagerAssignments
        (EmployeeId, ManagerId, Year, Month)
        VALUES
        (@empId, @managerId, @year, @month);
    ";

            cmd.Parameters.AddWithValue("@empId", assignment.EmployeeId);
            cmd.Parameters.AddWithValue("@managerId", assignment.ManagerId);
            cmd.Parameters.AddWithValue("@year", assignment.Year);
            cmd.Parameters.AddWithValue("@month", assignment.Month);

            cmd.ExecuteNonQuery();
        }

        // -----------------------------
        //  PRIVATE MAP METHOD
        // -----------------------------
        private static Employee Map(SqliteDataReader reader)
        {
            return new Employee
            {
                Id = reader.GetInt32(0),
                SicilNo = reader.GetInt32(1),
                FullName = reader.GetString(2),
                Role = (EmployeeRole)reader.GetInt32(3),
                ManagerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsActive = reader.GetInt32(5) == 1
            };
        }

        private void CreateLeaveBalanceIfMissing(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int employeeId)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"
        INSERT OR IGNORE INTO LeaveBalances (employee_id, year)
        VALUES (@empId, @year);
    ";

            cmd.Parameters.AddWithValue("@empId", employeeId);
            cmd.Parameters.AddWithValue("@year", DateTime.Now.Year);

            cmd.ExecuteNonQuery();
        }

        // -----------------------------
        // GET EMPLOYEES UNDER MANAGER FOR DATE
        // -----------------------------
        public List<Employee> GetEmployeesUnderManager(int managerId, DateTime date)
        {
            var list = new List<Employee>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        SELECT e.id, e.sicil_no, e.full_name, e.role, e.manager_id, e.is_active
        FROM Employees e
        INNER JOIN EmployeeManagerAssignments a
            ON a.EmployeeId = e.id
        WHERE a.ManagerId = @managerId
        AND a.Year = @year
        AND a.Month = @month
        AND e.is_active = 1
        ORDER BY e.full_name;
    ";

            cmd.Parameters.AddWithValue("@managerId", managerId);
            cmd.Parameters.AddWithValue("@year", date.Year);
            cmd.Parameters.AddWithValue("@month", date.Month);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        // -----------------------------
        // INSERT OR UPDATE ASSIGNMENTS 
        // -----------------------------

        public void SaveManagerAssignments(
    int employeeId,
    int managerId,
    int year,
    List<int> months)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var month in months)
                {
                    using var checkCmd = connection.CreateCommand();
                    checkCmd.Transaction = transaction;

                    checkCmd.CommandText = @"
                SELECT Id
                FROM EmployeeManagerAssignments
                WHERE EmployeeId = @empId
                AND Year = @year
                AND Month = @month
                LIMIT 1;
            ";

                    checkCmd.Parameters.AddWithValue("@empId", employeeId);
                    checkCmd.Parameters.AddWithValue("@year", year);
                    checkCmd.Parameters.AddWithValue("@month", month);

                    var result = checkCmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        // UPDATE
                        using var updateCmd = connection.CreateCommand();
                        updateCmd.Transaction = transaction;

                        updateCmd.CommandText = @"
                    UPDATE EmployeeManagerAssignments
                    SET ManagerId = @managerId
                    WHERE Id = @id;
                ";

                        updateCmd.Parameters.AddWithValue("@managerId", managerId);
                        updateCmd.Parameters.AddWithValue("@id", Convert.ToInt32(result));

                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // INSERT
                        using var insertCmd = connection.CreateCommand();
                        insertCmd.Transaction = transaction;

                        insertCmd.CommandText = @"
                    INSERT INTO EmployeeManagerAssignments
                    (EmployeeId, ManagerId, Year, Month)
                    VALUES
                    (@empId, @managerId, @year, @month);
                ";

                        insertCmd.Parameters.AddWithValue("@empId", employeeId);
                        insertCmd.Parameters.AddWithValue("@managerId", managerId);
                        insertCmd.Parameters.AddWithValue("@year", year);
                        insertCmd.Parameters.AddWithValue("@month", month);

                        insertCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // -----------------------------
        //  DELETE ASSIGNMENTS FOR EMPLOYEE-MANAGER-YEAR
        // -----------------------------
        public void DeleteManagerAssignments(int employeeId, int managerId, int year)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
        DELETE FROM EmployeeManagerAssignments
        WHERE EmployeeId = @empId
        AND ManagerId = @managerId
        AND Year = @year;
    ";

            cmd.Parameters.AddWithValue("@empId", employeeId);
            cmd.Parameters.AddWithValue("@managerId", managerId);
            cmd.Parameters.AddWithValue("@year", year);

            cmd.ExecuteNonQuery();
        }


        // -----------------------------
        //  RESTORE DELETED METHOD
        // -----------------------------
        public (Employee employee, bool WasRestored) RestoreOrCreate(
        int sicilNo,
        string fullName,
        EmployeeRole role,
        int? managerId = null)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {

                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = @"
            SELECT id, is_active
            FROM Employees
            WHERE sicil_no = @sicil_no;
        ";
                checkCmd.Parameters.AddWithValue("@sicil_no", sicilNo);

                using var reader = checkCmd.ExecuteReader();

                if (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    bool isActive = reader.GetInt32(1) == 1;

                    reader.Close();

                    if (!isActive)
                    {

                        using var restoreCmd = connection.CreateCommand();
                        restoreCmd.Transaction = transaction;
                        restoreCmd.CommandText = @"
                    UPDATE Employees
                    SET full_name = @full_name,
                        role = @role,
                        manager_id = @manager_id,
                        is_active = 1
                    WHERE id = @id;
                ";

                        restoreCmd.Parameters.AddWithValue("@full_name", fullName);
                        restoreCmd.Parameters.AddWithValue("@role", (int)role);
                        restoreCmd.Parameters.AddWithValue("@manager_id",
                            managerId.HasValue ? managerId : DBNull.Value);
                        restoreCmd.Parameters.AddWithValue("@id", id);

                        restoreCmd.ExecuteNonQuery();


                        CreateLeaveBalanceIfMissing(connection, transaction, id);

                        transaction.Commit();

                        return (new Employee
                        {
                            Id = id,
                            SicilNo = sicilNo,
                            FullName = fullName,
                            Role = role,
                            ManagerId = managerId,
                            IsActive = true
                        }, true);
                    }

                    throw new InvalidOperationException("Bu sicil numarası zaten kayıtlı.");
                }

                reader.Close();


                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
            INSERT INTO Employees (sicil_no, full_name, role, manager_id, is_active)
            VALUES (@sicil_no, @full_name, @role, @manager_id, 1);
        ";

                insertCmd.Parameters.AddWithValue("@sicil_no", sicilNo);
                insertCmd.Parameters.AddWithValue("@full_name", fullName);
                insertCmd.Parameters.AddWithValue("@role", (int)role);
                insertCmd.Parameters.AddWithValue("@manager_id",
                    managerId.HasValue ? managerId : DBNull.Value);

                insertCmd.ExecuteNonQuery();


                using var idCmd = connection.CreateCommand();
                idCmd.Transaction = transaction;
                idCmd.CommandText = "SELECT last_insert_rowid();";

                object? result = idCmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException("Yeni çalışan ID'si alınamadı.");

                int newId = Convert.ToInt32(result);


                CreateLeaveBalanceIfMissing(connection, transaction, newId);

                transaction.Commit();

                return (new Employee
                {
                    Id = newId,
                    SicilNo = sicilNo,
                    FullName = fullName,
                    Role = role,
                    ManagerId = managerId,
                    IsActive = true
                }, false);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

    }
}