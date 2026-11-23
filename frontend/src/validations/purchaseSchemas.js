import { z } from 'zod';

export const purchaseItemSchema = z.object({
  productId: z.union([z.string(), z.number()]),
  productName: z.string(),
  sku: z.string().optional(),
  quantity: z.coerce.number().min(1, 'Quantity must be at least 1'),
  unitCost: z.coerce.number().min(0, 'Cost must be positive'),
  unit: z.string().optional(),
});

export const purchaseSchema = z.object({
  supplierId: z.union([z.string(), z.number()]).refine(val => !!val, 'Supplier is required'),
  invoiceNumber: z.string().min(1, 'Supplier Invoice / Bill number is required'),
  purchaseDate: z.string().min(1, 'Purchase date is required'),
  notes: z.string().optional(),
  items: z.array(purchaseItemSchema).min(1, 'At least one item must be added'),
});

