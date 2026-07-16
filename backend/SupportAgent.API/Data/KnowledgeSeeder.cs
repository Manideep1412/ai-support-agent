using MongoDB.Driver;
using SupportAgent.API.Models.Entities;

namespace SupportAgent.API.Data;

/// <summary>
/// Seeds sample knowledge-base articles on first startup.
/// Embeddings are generated separately via POST /api/knowledge/embed-all.
/// </summary>
public static class KnowledgeSeeder
{
    public static async Task SeedAsync(MongoDbContext db)
    {
        if (await db.Articles.CountDocumentsAsync(FilterDefinition<KnowledgeArticle>.Empty) > 0)
            return;

        var articles = new List<KnowledgeArticle>
        {
            new() {
                Title    = "How to reset your password",
                Category = "Account",
                Content  = "To reset your password, go to the login page and click 'Forgot Password'. " +
                           "Enter your registered email address and we'll send you a reset link within 2 minutes. " +
                           "The link expires in 24 hours. If you don't receive the email, check your spam folder. " +
                           "For accounts using SSO (Google / Microsoft), reset password directly with your identity provider."
            },
            new() {
                Title    = "Billing and accepted payment methods",
                Category = "Billing",
                Content  = "We accept Visa, Mastercard, American Express, and PayPal. " +
                           "Invoices are generated on the 1st of each month and emailed automatically. " +
                           "You can update your payment method under Account Settings › Billing. " +
                           "If a payment fails, we'll retry 3 times over 7 days before suspending the account. " +
                           "Annual plans receive a 20 % discount compared to monthly pricing."
            },
            new() {
                Title    = "Cancellation and refund policy",
                Category = "Billing",
                Content  = "You can cancel your subscription at any time from Account Settings › Subscription. " +
                           "Cancellation takes effect at the end of the current billing period — you retain access until then. " +
                           "We offer a 30-day money-back guarantee for new customers on any paid plan. " +
                           "Refunds are processed within 5–7 business days to the original payment method. " +
                           "Partial refunds are not available for mid-cycle cancellations."
            },
            new() {
                Title    = "How to export your data",
                Category = "Technical",
                Content  = "To export your data go to Settings › Data & Privacy › Export Data. " +
                           "Supported formats are CSV and JSON. Large exports (>10 MB) may take up to 30 minutes. " +
                           "You'll receive an email with a download link once the export is ready. " +
                           "Download links expire after 48 hours. " +
                           "Per GDPR you can request a full data export once every 30 days."
            },
            new() {
                Title    = "API access and rate limits",
                Category = "Technical",
                Content  = "API keys can be generated under Settings › Developer › API Keys. " +
                           "Each key has its own rate limit: Free plan = 100 requests/min, Pro = 1 000 requests/min, Enterprise = unlimited. " +
                           "We support REST and GraphQL. Webhooks are available for real-time event notifications. " +
                           "Full API reference is at docs.example.com. SDKs are available for Python, Node.js, and .NET."
            },
            new() {
                Title    = "Team collaboration and roles",
                Category = "General",
                Content  = "Invite team members under Settings › Team › Invite Member. " +
                           "Free plans support up to 3 seats. Pro plans are unlimited. " +
                           "Available roles: Admin (full access including billing), Editor (create and modify content), Viewer (read-only). " +
                           "Admins can transfer ownership, manage billing, and remove members. " +
                           "Pending invitations expire after 7 days."
            },
            new() {
                Title    = "Mobile app setup",
                Category = "General",
                Content  = "Our mobile app is available on iOS (App Store) and Android (Google Play) — search 'SupportAgent'. " +
                           "Sign in with your existing account credentials. " +
                           "Enable push notifications for real-time alerts on new messages and billing events. " +
                           "The app supports offline mode; content is cached for up to 72 hours. " +
                           "Biometric authentication (Face ID / fingerprint) can be enabled in the app settings."
            },
            new() {
                Title    = "Data security and compliance",
                Category = "Technical",
                Content  = "All data is encrypted at rest (AES-256) and in transit (TLS 1.3). " +
                           "We are SOC 2 Type II certified and GDPR / CCPA compliant. " +
                           "Data residency options: EU (Frankfurt) or US (Virginia) — configurable at account creation. " +
                           "Two-factor authentication (TOTP / SMS) is available and strongly recommended. " +
                           "Security audit logs are available to Admins under Settings › Security › Audit Log. " +
                           "We never sell or share personal data with third parties."
            },
        };

        await db.Articles.InsertManyAsync(articles);
    }
}
