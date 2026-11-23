/**
 * Format currency in Indian Rupees (INR)
 */
export function formatCurrency(amount) {
  if (amount === undefined || amount === null || isNaN(amount)) return '₹0.00';
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
  }).format(amount);
}

/**
 * Format numbers in Indian numbering system
 */
export function formatNumber(number) {
  if (number === undefined || number === null || isNaN(number)) return '0';
  return new Intl.NumberFormat('en-IN').format(number);
}

/**
 * Format dates
 */
export function formatDate(dateString) {
  if (!dateString) return '-';
  try {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(date);
  } catch {
    return dateString;
  }
}

/**
 * Format datetime
 */
export function formatDateTime(dateString) {
  if (!dateString) return '-';
  try {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  } catch {
    return dateString;
  }
}

