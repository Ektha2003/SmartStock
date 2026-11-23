namespace SmartStock.Core.Enums;

public enum UserRole
{
    Owner = 1,
    Staff = 2
}

public enum UnitType
{
    Piece = 1,
    Kg = 2,
    Gram = 3,
    Litre = 4,
    Box = 5,
    Packet = 6
}

public enum TransactionType
{
    Opening = 1,
    Purchase = 2,
    Sale = 3,
    Adjustment = 4
}

public enum NotificationType
{
    LowStock = 1,
    OutOfStock = 2,
    ReorderRecommendation = 3,
    System = 4
}

public enum StockRiskLevel
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Healthy = 4
}
