using System;
using System.Linq;
using RefactorThis.Persistence.Interface;
using RefactorThis.Persistence.Model;

namespace RefactorThis.Domain
{
    public class InvoiceService
	{
        private IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public string ProcessPayment(Payment payment)
        {
            var invoice = _invoiceRepository.GetInvoice(payment.Reference);

            return ProcessPaymentMessage(invoice, payment); 
             
        }

        private string HandleZeroAmountInvoice(Invoice invoice)
        {
            if (invoice.Payments == null || !invoice.Payments.Any())
            {
                return "no payment needed";
            }

            throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");
        }

        private string ProcessFinalPayment(Invoice invoice, Payment payment)
        {
            UpdateInvoicePayment(invoice, payment);

            return "final partial payment received, invoice is now fully paid";
        }

        private string ProcessPartialPayment(Invoice invoice, Payment payment)
        {
            UpdateInvoicePayment(invoice, payment);

            return invoice.Payments.Count == 1
                ? "invoice is now partially paid"
                : "another partial payment received, still not fully paid";
        }

        private void UpdateInvoicePayment(Invoice invoice, Payment payment)
        {
            invoice.AmountPaid += payment.Amount;

            if (invoice.Type == InvoiceType.Commercial)
            {
                invoice.TaxAmount += payment.Amount * 0.14m;
            }

            invoice.Payments.Add(payment);
            invoice.Save();
        }
        
        private string ProcessPaymentMessage(Invoice invoice, Payment payment)
        {
            string paymentMessage = string.Empty;

            if (invoice == null)
            {
                throw new InvalidOperationException("There is no invoice matching this payment");
            }

            if (invoice.Amount == 0)
            {
                if (invoice.Payments == null || !invoice.Payments.Any())
                {
                    return "no payment needed";
                }

                throw new InvalidOperationException("The invoice is in an invalid state: it has an amount of 0 but it has payments.");
            }

            var totalPaid = invoice.Payments?.Sum(x => x.Amount) ?? 0;
            var remainingAmount = invoice.Amount - invoice.AmountPaid;

            if (totalPaid == invoice.Amount)
            {
                return "invoice was already fully paid";
            }

            if (invoice.Payments == null || !invoice.Payments.Any())
            {
                // Initial payment scenario
                if (payment.Amount > invoice.Amount)
                {
                    return "the payment is greater than the invoice amount";
                }
            }
            else
            {
                // Subsequent payments
                if (payment.Amount > remainingAmount)
                {
                    return "the payment is greater than the partial amount remaining";
                }
            }

            // Handle the final or partial payment processing
            return payment.Amount == remainingAmount
                ? ProcessFinalPayment(invoice, payment)
                : ProcessPartialPayment(invoice, payment); 
        }
    }
}