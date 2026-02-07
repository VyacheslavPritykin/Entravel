namespace OrderProcessing.API.Extensions;

public static class SqlExceptionExtensions
{
    public static bool IsUniqueKeyViolation(this DbUpdateException ex) =>
        ex.InnerException is DbException dbEx && dbEx.IsUniqueKeyViolation();

    public static bool IsUniqueKeyViolation(this DbException ex) =>
        // 23000: integrity_constraint_violation
        // 23505: unique_violation
        ex is Npgsql.PostgresException { SqlState: "23000" or "23505" };
    
    public static bool IsForeignKeyViolation(this DbUpdateException ex) =>
        ex.InnerException is DbException dbEx && dbEx.IsForeignKeyViolation();

    public static bool IsForeignKeyViolation(this DbException ex) =>
        // 23503: foreign_key_violation
        ex is Npgsql.PostgresException { SqlState: "23503" };
    
    public static bool IsCheckConstraintViolation(this DbUpdateException ex, string? constraintName = null) =>
        ex.InnerException is DbException dbEx && dbEx.IsCheckConstraintViolation(constraintName);

    public static bool IsCheckConstraintViolation(this DbException ex, string? constraintName = null) =>
        // 23514: check_violation
        ex is Npgsql.PostgresException { SqlState: "23514" } exception &&
        (string.IsNullOrEmpty(constraintName) || exception.ConstraintName == constraintName);

}