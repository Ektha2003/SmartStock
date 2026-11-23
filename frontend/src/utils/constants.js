export const UserRole = {
  Owner: 1,
  Staff: 2,
};

export const UserRoleName = {
  1: 'Owner',
  2: 'Staff',
};

export const UnitType = {
  Piece: 1,
  Kg: 2,
  Gram: 3,
  Litre: 4,
  Box: 5,
  Packet: 6,
};

export const UnitTypeName = {
  1: 'Piece',
  2: 'Kg',
  3: 'Gram',
  4: 'Litre',
  5: 'Box',
  6: 'Packet',
};

export const TransactionType = {
  Opening: 1,
  Purchase: 2,
  Sale: 3,
  Adjustment: 4,
};

export const TransactionTypeName = {
  1: 'Opening Stock',
  2: 'Purchase',
  3: 'Sale',
  4: 'Adjustment',
};

export const NotificationType = {
  LowStock: 1,
  OutOfStock: 2,
  ReorderRecommendation: 3,
  System: 4,
};

export const NotificationTypeName = {
  1: 'LowStock',
  2: 'OutOfStock',
  3: 'ReorderRecommendation',
  4: 'System',
};

export const StockRiskLevel = {
  Critical: 1,
  High: 2,
  Medium: 3,
  Healthy: 4,
};

export const StockRiskLevelName = {
  1: 'Critical',
  2: 'High',
  3: 'Medium',
  4: 'Healthy',
};

export function getUnitName(unit) {
  if (typeof unit === 'number') {
    return UnitTypeName[unit] || 'Piece';
  }
  if (!unit) return 'Piece';
  const match = Object.entries(UnitType).find(
    ([k]) => k.toLowerCase() === String(unit).toLowerCase()
  );
  return match ? match[0] : String(unit);
}

export function getUnitId(unit) {
  if (typeof unit === 'number') return unit;
  if (!unit) return UnitType.Piece;
  const lower = String(unit).toLowerCase();
  for (const [key, val] of Object.entries(UnitType)) {
    if (key.toLowerCase() === lower) return val;
  }
  if (lower === 'packet' || lower === 'pkt') return UnitType.Packet;
  if (lower === 'liter' || lower === 'litre' || lower === 'l') return UnitType.Litre;
  if (lower === 'box') return UnitType.Box;
  if (lower === 'kg') return UnitType.Kg;
  if (lower === 'gram' || lower === 'g') return UnitType.Gram;
  return UnitType.Piece;
}

