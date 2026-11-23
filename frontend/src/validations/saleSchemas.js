import { z } from 'zod';

export const saleItemSchema = z.object({
  productId: z.union([z.string(), z.number()]),
  productName: z.string(),
  sku: z.string().optional(),
  quantity: z.coerce.number().min(1, 'Quantity must be at least 1'),
  unitPrice: z.coerce.number().min(0, 'Price must be positive'),
  availableStock: z.number().optional(),
  unit: z.string().optional(),
});

export const saleSchema = z.object({
  customerName: z.string().optional(),
  customerPhone: z.string().optional(),
  notes: z.string().optional(),
  paymentMethod: z.enum(['Cash', 'UPI', 'Card', 'Credit / Udhaar']).default('Cash'),
  items: z.array(saleItemSchema).min(1, 'At least one product must be added to the sale'),
});

