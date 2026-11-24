import { z } from 'zod';

export const supplierSchema = z.object({
  name: z.string().min(2, 'Supplier name is required'),
  companyName: z.string().optional(),
  contactPerson: z.string().min(2, 'Contact person name is required'),
  phone: z.string().min(10, 'Valid 10-digit phone number is required'),
  email: z.string().email('Valid email address is required').optional().or(z.literal('')),
  address: z.string().min(3, 'Address is required'),
  billingAddress: z.string().optional(),
  gstin: z.string().optional(),
  panNumber: z.string().optional(),
  paymentTerms: z.string().optional(),
  notes: z.string().optional(),
  status: z.enum(['Active', 'Inactive']).default('Active'),
});

