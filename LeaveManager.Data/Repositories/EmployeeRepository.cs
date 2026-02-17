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

            using var cmd = connection.CreateCommand();
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

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Bu sicil numarası zaten kayıtlı.");
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

            // Check if employee exists
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = @"
        SELECT id, full_name, role, manager_id, is_active
        FROM Employees
        WHERE sicil_no = @sicil_no;
    ";
            checkCmd.Parameters.AddWithValue("@sicil_no", sicilNo);

            using var reader = checkCmd.ExecuteReader();

            if (reader.Read())
            {
                int id = reader.GetInt32(0);
                bool isActive = reader.GetInt32(4) == 1;

                if (!isActive)
                {
                    reader.Close();

                    using var restoreCmd = connection.CreateCommand();
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

                    var restoredEmployee = new Employee
                    {
                        Id = id,
                        SicilNo = sicilNo,
                        FullName = fullName,
                        Role = role,
                        ManagerId = managerId,
                        IsActive = true
                    };

                    return (restoredEmployee, true);
                }
                else
                {
                    throw new InvalidOperationException("Bu sicil numarası zaten kayıtlı.");
                }
            }

            reader.Close();

            // Insert new employee
            using var insertCmd = connection.CreateCommand();
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

            // Get last inserted id (SAFE)
            using var lastIdCmd = connection.CreateCommand();
            lastIdCmd.CommandText = "SELECT last_insert_rowid();";

            var result = lastIdCmd.ExecuteScalar();

            if (result == null)
                throw new InvalidOperationException("Yeni çalışan ID'si alınamadı.");

            long newId = Convert.ToInt64(result);

            var newEmployee = new Employee
            {
                Id = (int)newId,
                SicilNo = sicilNo,
                FullName = fullName,
                Role = role,
                ManagerId = managerId,
                IsActive = true
            };

            return (newEmployee, false);
        }
    
    }
}
    
