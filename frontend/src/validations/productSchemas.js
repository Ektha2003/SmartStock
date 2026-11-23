import { z } from 'zod';

export const productSchema = z.object({
  name: z.string().min(2, 'Product name is required'),
  sku: z.string().min(2, 'SKU / Barcode is required'),
  categoryId: z.union([z.string(), z.number()]).refine(val => !!val, 'Category is required'),
  description: z.string().optional(),
  purchasePrice: z.coerce.number().min(0, 'Purchase price must be positive'),
  sellingPrice: z.coerce.number().min(0, 'Selling price must be positive'),
  currentStock: z.coerce.number().min(0, 'Initial stock cannot be negative').default(0),
  minStock: z.coerce.number().min(0, 'Minimum stock cannot be negative').default(5),
  unit: z.union([z.coerce.number(), z.string()]).default(1),
  status: z.enum(['Active', 'Inactive']).default('Active'),
});
