// High quality realistic Indian Kirana Store data repository adhering to backend DTO contracts

const STORAGE_KEYS = {
  PRODUCTS: 'smartstock_db_products',
  CATEGORIES: 'smartstock_db_categories',
  SUPPLIERS: 'smartstock_db_suppliers',
  SALES: 'smartstock_db_sales',
  PURCHASES: 'smartstock_db_purchases',
  TRANSACTIONS: 'smartstock_db_transactions',
  NOTIFICATIONS: 'smartstock_db_notifications',
  BUSINESS: 'smartstock_db_business',
};

const initialBusiness = {
  id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  businessName: "Sharma Kirana & General Store",
  ownerName: "Ramesh Sharma",
  phone: "+91 9810123456",
  address: "Shop 12, Main Market, Chandni Chowk, Delhi 110006",
  businessType: "Kirana / Grocery",
  isActive: true,
};

const initialCategories = [
  { id: "cat-1", name: "Grains & Staples", description: "Flour, rice, pulses, and cooking grains", productCount: 4 },
  { id: "cat-2", name: "Edible Oils & Ghee", description: "Mustard oil, Refined oil, Desi Ghee", productCount: 2 },
  { id: "cat-3", name: "Dairy & Breakfast", description: "Butter, Milk powder, Bread, Oats", productCount: 2 },
  { id: "cat-4", name: "Snacks & Biscuits", description: "Biscuits, Maggi, Namkeen, Chips", productCount: 2 },
  { id: "cat-5", name: "Beverages & Tea", description: "Tea powders, coffee, and soft drinks", productCount: 2 },
  { id: "cat-6", name: "Cleaning & Household", description: "Detergents, Dishwash, Floor cleaners", productCount: 1 },
];

const initialSuppliers = [
  {
    id: "sup-1",
    supplierName: "Delhi Wholesale FMCG Traders",
    contactPerson: "Sunil Gupta",
    phone: "+91 9811223344",
    email: "orders@delhifmcg.com",
    address: "Kari Baoli, Khari Baoli Wholesale Market, Delhi",
    isActive: true,
  },
  {
    id: "sup-2",
    supplierName: "Delhi Grain & Flour Depot",
    contactPerson: "Harish Chandra",
    phone: "+91 9822345678",
    email: "delhigrain@depot.com",
    address: "Plot 44, Naya Bazar, Old Delhi",
    isActive: true,
  },
  {
    id: "sup-3",
    supplierName: "Amul North Dairy Depot",
    contactPerson: "Vikram Singh",
    phone: "+91 9822334455",
    email: "depot.north@amul.in",
    address: "Okhla Industrial Area Phase 1, Delhi",
    isActive: true,
  },
];

const initialProducts = [
  {
    id: "prod-1",
    categoryId: "cat-1",
    categoryName: "Grains & Staples",
    name: "Aashirvaad Shudh Chakki Atta 10kg",
    sku: "AASH-ATTA-10K",
    description: "100% whole wheat flour",
    purchasePrice: 420.00,
    sellingPrice: 475.00,
    currentStock: 35.0,
    minimumStock: 10.0,
    unit: 1, // Piece
    unitName: "Piece",
    isActive: true,
    isLowStock: false,
    isOutOfStock: false,
    avgDailySales: 4.2,
  },
  {
    id: "prod-2",
    categoryId: "cat-1",
    categoryName: "Grains & Staples",
    name: "Tata Salt Vacuum Evaporated 1kg",
    sku: "TATA-SALT-1K",
    description: "Desh Ka Namak, enriched with iodine",
    purchasePrice: 22.00,
    sellingPrice: 28.00,
    currentStock: 4.0,
    minimumStock: 15.0,
    unit: 6, // Packet
    unitName: "Packet",
    isActive: true,
    isLowStock: true,
    isOutOfStock: false,
    avgDailySales: 5.5,
  },
  {
    id: "prod-3",
    categoryId: "cat-2",
    categoryName: "Edible Oils & Ghee",
    name: "Fortune Sunlite Refined Sunflower Oil 1L",
    sku: "FORT-OIL-1L",
    description: "Light and healthy refined sunflower cooking oil",
    purchasePrice: 125.00,
    sellingPrice: 165.00,
    currentStock: 4.0,
    minimumStock: 12.0,
    unit: 4, // Litre
    unitName: "Litre",
    isActive: true,
    isLowStock: true,
    isOutOfStock: false,
    avgDailySales: 3.1,
  },
  {
    id: "prod-4",
    categoryId: "cat-1",
    categoryName: "Grains & Staples",
    name: "India Gate Basmati Rice Feast Rozzana 1kg",
    sku: "RICE-FEAST-1K",
    description: "Aromatic basmati rice for daily meals",
    purchasePrice: 85.00,
    sellingPrice: 105.00,
    currentStock: 25.0,
    minimumStock: 10.0,
    unit: 2, // Kg
    unitName: "Kg",
    isActive: true,
    isLowStock: false,
    isOutOfStock: false,
    avgDailySales: 2.8,
  },
  {
    id: "prod-5",
    categoryId: "cat-4",
    categoryName: "Snacks & Biscuits",
    name: "Maggi 2-Minute Masala Noodles 70g (Pack of 4)",
    sku: "MAGGI-MAS-4PK",
    description: "Classic favorite masala instant noodles",
    purchasePrice: 48.00,
    sellingPrice: 56.00,
    currentStock: 2.0,
    minimumStock: 20.0,
    unit: 6, // Packet
    unitName: "Packet",
    isActive: true,
    isLowStock: true,
    isOutOfStock: false,
    avgDailySales: 6.0,
  },
  {
    id: "prod-6",
    categoryId: "cat-3",
    categoryName: "Dairy & Breakfast",
    name: "Amul Butter Pasteurised 500g",
    sku: "AMUL-BUTR-500",
    description: "Utterly butterly delicious creamy table butter",
    purchasePrice: 245.00,
    sellingPrice: 275.00,
    currentStock: 0.0,
    minimumStock: 8.0,
    unit: 1, // Piece
    unitName: "Piece",
    isActive: true,
    isLowStock: true,
    isOutOfStock: true,
    avgDailySales: 2.5,
  },
  {
    id: "prod-7",
    categoryId: "cat-4",
    categoryName: "Snacks & Biscuits",
    name: "Parle-G Original Glucose Biscuits 800g Mega Pack",
    sku: "PARL-GLUC-800",
    description: "India's largest selling glucose biscuit",
    purchasePrice: 65.00,
    sellingPrice: 80.00,
    currentStock: 30.0,
    minimumStock: 12.0,
    unit: 6, // Packet
    unitName: "Packet",
    isActive: true,
    isLowStock: false,
    isOutOfStock: false,
    avgDailySales: 3.8,
  },
  {
    id: "prod-8",
    categoryId: "cat-5",
    categoryName: "Beverages & Tea",
    name: "Tata Tea Gold 500g",
    sku: "TATA-TEA-500",
    description: "Premium tea leaves blend with natural aroma",
    purchasePrice: 280.00,
    sellingPrice: 330.00,
    currentStock: 18.0,
    minimumStock: 5.0,
    unit: 6, // Packet
    unitName: "Packet",
    isActive: true,
    isLowStock: false,
    isOutOfStock: false,
    avgDailySales: 1.8,
  },
];

const initialSales = [
  {
    id: "sale-101",
    invoiceNumber: "INV-20261123-0001",
    saleDate: "2026-11-23T10:00:00Z",
    totalAmount: 1115.00,
    notes: "Cash counter checkout",
    customerName: "Sanjay Sharma",
    customerPhone: "+91 9871100222",
    paymentMethod: "UPI",
    items: [
      {
        id: "sitem-1",
        productId: "prod-1",
        productName: "Aashirvaad Shudh Chakki Atta 10kg",
        sku: "AASH-ATTA-10K",
        quantity: 2,
        unitPrice: 475.00,
        subTotal: 950.00,
      },
      {
        id: "sitem-2",
        productId: "prod-3",
        productName: "Fortune Sunlite Refined Sunflower Oil 1L",
        sku: "FORT-OIL-1L",
        quantity: 1,
        unitPrice: 165.00,
        subTotal: 165.00,
      },
    ],
  },
  {
    id: "sale-102",
    invoiceNumber: "INV-20261123-0002",
    saleDate: "2026-11-23T11:30:00Z",
    totalAmount: 3735.00,
    notes: "Counter billing",
    customerName: "Priya Mehra",
    customerPhone: "+91 9810998877",
    paymentMethod: "Cash",
    items: [
      {
        id: "sitem-3",
        productId: "prod-4",
        productName: "India Gate Basmati Rice Feast Rozzana 1kg",
        sku: "RICE-FEAST-1K",
        quantity: 5,
        unitPrice: 105.00,
        subTotal: 525.00,
      },
      {
        id: "sitem-4",
        productId: "prod-8",
        productName: "Tata Tea Gold 500g",
        sku: "TATA-TEA-500",
        quantity: 2,
        unitPrice: 330.00,
        subTotal: 660.00,
      },
      {
        id: "sitem-5",
        productId: "prod-6",
        productName: "Amul Butter Pasteurised 500g",
        sku: "AMUL-BUTR-500",
        quantity: 9,
        unitPrice: 275.00,
        subTotal: 2475.00,
      },
    ],
  },
];

const initialPurchases = [
  {
    id: "purch-201",
    invoiceNumber: "BILL-LK-9082",
    supplierId: "sup-1",
    supplierName: "Delhi Wholesale FMCG Traders",
    purchaseDate: "2026-11-23T11:00:00Z",
    totalAmount: 11200.00,
    notes: "Weekly milk & dairy replenishment",
    items: [
      {
        id: "pitem-1",
        productId: "prod-1",
        productName: "Aashirvaad Shudh Chakki Atta 10kg",
        sku: "AASH-ATTA-10K",
        quantity: 20,
        unitCost: 420.00,
        subTotal: 8400.00,
      },
      {
        id: "pitem-2",
        productId: "prod-8",
        productName: "Tata Tea Gold 500g",
        sku: "TATA-TEA-500",
        quantity: 10,
        unitCost: 280.00,
        subTotal: 2800.00,
      },
    ],
  },
];

const initialTransactions = [
  {
    id: "tx-1",
    productId: "prod-2",
    productName: "Tata Salt Vacuum Evaporated 1kg",
    sku: "TATA-SALT-1K",
    transactionType: 3, // Sale
    transactionTypeName: "Sale",
    quantity: -2.0,
    previousStock: 6.0,
    newStock: 4.0,
    referenceId: "sale-101",
    notes: "Invoice INV-20261123-0001",
    createdAt: "2026-11-23T10:00:00Z",
  },
  {
    id: "tx-2",
    productId: "prod-1",
    productName: "Aashirvaad Shudh Chakki Atta 10kg",
    sku: "AASH-ATTA-10K",
    transactionType: 3, // Sale
    transactionTypeName: "Sale",
    quantity: -2.0,
    previousStock: 37.0,
    newStock: 35.0,
    referenceId: "sale-101",
    notes: "Invoice INV-20261123-0001",
    createdAt: "2026-11-23T10:00:00Z",
  },
  {
    id: "tx-3",
    productId: "prod-6",
    productName: "Amul Butter Pasteurised 500g",
    sku: "AMUL-BUTR-500",
    transactionType: 4, // Adjustment
    transactionTypeName: "Adjustment",
    quantity: -9.0,
    previousStock: 9.0,
    newStock: 0.0,
    referenceId: "adj-manual",
    notes: "Expired stock discarded during refrigeration defrosting",
    createdAt: "2026-11-22T16:00:00Z",
  },
  {
    id: "tx-4",
    productId: "prod-1",
    productName: "Aashirvaad Shudh Chakki Atta 10kg",
    sku: "AASH-ATTA-10K",
    transactionType: 2, // Purchase
    transactionTypeName: "Purchase",
    quantity: 20.0,
    previousStock: 17.0,
    newStock: 37.0,
    referenceId: "purch-201",
    notes: "Bill BILL-LK-9082",
    createdAt: "2026-11-22T11:00:00Z",
  },
  {
    id: "tx-5",
    productId: "prod-8",
    productName: "Tata Tea Gold 500g",
    sku: "TATA-TEA-500",
    transactionType: 1, // Opening
    transactionTypeName: "Opening",
    quantity: 18.0,
    previousStock: 0.0,
    newStock: 18.0,
    referenceId: "opening-001",
    notes: "Initial inventory setup",
    createdAt: "2026-11-20T08:00:00Z",
  },
];

const initialNotifications = [
  {
    id: "notif-1",
    productId: "prod-6",
    productName: "Amul Butter Pasteurised 500g",
    title: "Out of Stock: Amul Butter Pasteurised 500g",
    message: "Current stock (0 Piece) is exhausted. Reorder recommended.",
    notificationType: 2, // OutOfStock
    notificationTypeName: "OutOfStock",
    isRead: false,
    createdAt: "2026-11-23T09:00:00Z",
  },
  {
    id: "notif-2",
    productId: "prod-2",
    productName: "Tata Salt Vacuum Evaporated 1kg",
    title: "Low Stock Alert: Tata Salt Vacuum Evaporated 1kg",
    message: "Current stock (4.00 Packet) is below minimum threshold (15.00 Packet).",
    notificationType: 1, // LowStock
    notificationTypeName: "LowStock",
    isRead: false,
    createdAt: "2026-11-23T10:00:00Z",
  },
  {
    id: "notif-3",
    productId: "prod-3",
    productName: "Fortune Sunlite Refined Sunflower Oil 1L",
    title: "Low Stock Alert: Fortune Sunlite Refined Sunflower Oil 1L",
    message: "Current stock (4.00 Litre) is below minimum threshold (12.00 Litre).",
    notificationType: 1, // LowStock
    notificationTypeName: "LowStock",
    isRead: false,
    createdAt: "2026-11-23T10:15:00Z",
  },
];

export const getStoreData = (key, initial) => {
  try {
    const data = localStorage.getItem(key);
    if (!data) {
      localStorage.setItem(key, JSON.stringify(initial));
      return initial;
    }
    return JSON.parse(data);
  } catch {
    return initial;
  }
};

export const setStoreData = (key, data) => {
  localStorage.setItem(key, JSON.stringify(data));
};

export const mockDb = {
  getBusiness: () => getStoreData(STORAGE_KEYS.BUSINESS, initialBusiness),
  saveBusiness: (data) => setStoreData(STORAGE_KEYS.BUSINESS, data),

  getProducts: () => getStoreData(STORAGE_KEYS.PRODUCTS, initialProducts),
  saveProducts: (data) => setStoreData(STORAGE_KEYS.PRODUCTS, data),

  getCategories: () => getStoreData(STORAGE_KEYS.CATEGORIES, initialCategories),
  saveCategories: (data) => setStoreData(STORAGE_KEYS.CATEGORIES, data),

  getSuppliers: () => getStoreData(STORAGE_KEYS.SUPPLIERS, initialSuppliers),
  saveSuppliers: (data) => setStoreData(STORAGE_KEYS.SUPPLIERS, data),

  getSales: () => getStoreData(STORAGE_KEYS.SALES, initialSales),
  saveSales: (data) => setStoreData(STORAGE_KEYS.SALES, data),

  getPurchases: () => getStoreData(STORAGE_KEYS.PURCHASES, initialPurchases),
  savePurchases: (data) => setStoreData(STORAGE_KEYS.PURCHASES, data),

  getTransactions: () => getStoreData(STORAGE_KEYS.TRANSACTIONS, initialTransactions),
  saveTransactions: (data) => setStoreData(STORAGE_KEYS.TRANSACTIONS, data),

  getNotifications: () => getStoreData(STORAGE_KEYS.NOTIFICATIONS, initialNotifications),
  saveNotifications: (data) => setStoreData(STORAGE_KEYS.NOTIFICATIONS, data),
};
