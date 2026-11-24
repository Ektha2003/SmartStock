import React, { useState } from 'react';
import { createPortal } from 'react-dom';
import { Upload, FileText, Sparkles, Loader2, X, CheckCircle2 } from 'lucide-react';
import aiService from '../../services/aiService';
import { toast } from 'sonner';

export function ScanBillModal({ isOpen, onClose, onExtracted }) {
  const [file, setFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [isScanning, setIsScanning] = useState(false);

  if (!isOpen) return null;

  const handleFileChange = (e) => {
    const selected = e.target.files[0];
    if (selected) {
      setFile(selected);
      if (selected.type.startsWith('image/')) {
        setPreviewUrl(URL.createObjectURL(selected));
      } else {
        setPreviewUrl(null);
      }
    }
  };

  const handleScan = async () => {
    if (!file) {
      toast.error('Please select an invoice bill image or PDF');
      return;
    }

    setIsScanning(true);
    try {
      const extracted = await aiService.extractInvoiceFromBill(file);
      toast.success('Invoice data successfully extracted by SmartStock AI!');
      onExtracted(extracted);
      onClose();
    } catch (err) {
      toast.error(err.message || 'Failed to scan and extract invoice');
    } finally {
      setIsScanning(false);
    }
  };

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div
        onClick={onClose}
        className="fixed -inset-10 bg-slate-900/50 backdrop-blur-sm"
      />
      <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100 relative z-10 animate-in fade-in">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 transition"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex items-center gap-2.5 mb-2">
          <div className="p-2.5 rounded-xl bg-indigo-50 text-indigo-700">
            <Sparkles className="w-5 h-5 text-indigo-600" />
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">Scan Paper Invoice Bill (AI OCR)</h3>
            <p className="text-xs text-slate-500">Upload a vendor receipt or supplier delivery bill</p>
          </div>
        </div>

        <div className="mt-4 space-y-4">
          <div className="border-2 border-dashed border-slate-200 rounded-xl p-6 text-center hover:border-indigo-400 transition bg-slate-50/50">
            <input
              type="file"
              id="bill-upload"
              accept="image/*,application/pdf"
              onChange={handleFileChange}
              className="hidden"
            />
            <label htmlFor="bill-upload" className="cursor-pointer flex flex-col items-center">
              {previewUrl ? (
                <div className="relative mb-3">
                  <img src={previewUrl} alt="Bill Preview" className="h-32 object-contain rounded-lg shadow-xs" />
                  <span className="block text-[11px] text-indigo-600 font-semibold mt-1">Click to change file</span>
                </div>
              ) : (
                <div className="w-12 h-12 rounded-full bg-indigo-100 text-indigo-600 flex items-center justify-center mb-3">
                  <Upload className="w-6 h-6" />
                </div>
              )}
              <p className="text-xs font-semibold text-slate-800">
                {file ? file.name : 'Drop photo of vendor bill or browse file'}
              </p>
              <p className="text-[11px] text-slate-400 mt-1">Supports JPG, PNG, PDF receipts</p>
            </label>
          </div>

          <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 text-xs text-slate-600 space-y-1">
            <p className="font-semibold text-slate-800 flex items-center gap-1.5">
              <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              Automated Extraction Features:
            </p>
            <ul className="list-disc list-inside text-slate-500 space-y-0.5 pl-1">
              <li>Wholesaler Name & Invoice Bill Number</li>
              <li>Line items, delivered quantities, and unit wholesale cost</li>
              <li>Pre-fills Purchase Form for 1-click stock inward</li>
            </ul>
          </div>

          <div className="pt-2 flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isScanning}
              className="px-4 py-2 text-xs font-semibold text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleScan}
              disabled={isScanning || !file}
              className="px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm flex items-center gap-2 disabled:opacity-50"
            >
              {isScanning ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" /> Scanning with AI OCR...
                </>
              ) : (
                <>
                  <Sparkles className="w-3.5 h-3.5 text-amber-300" /> Extract & Pre-fill Bill
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body
  );
}

export default ScanBillModal;

