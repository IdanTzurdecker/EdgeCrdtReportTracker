
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;


/// <summary>
/// Audit log entry following NIST SP 800-53 Control AU-3 requirements.
/// Implements cryptographic hash chaining 
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for entry.
    /// </summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; set; }

    /// <summary>
    /// ISO 8601 timestamp 
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Identity of the actor that performed the action.
    /// </summary>
    [JsonPropertyName("actorId")]
    public string ActorId { get; set; }

    /// <summary>
    /// Type of action performed 
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; }

    /// <summary>
    /// Resource identifier 
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string ResourceId { get; set; }

    /// <summary>
    /// Outcome of the action (SUCCESS, FAILURE, CONFLICT_RESOLVED).
    /// </summary>
    [JsonPropertyName("outcome")]
    public string Outcome { get; set; }

    /// <summary>
    /// Classification level of the data involved.
    /// </summary>
    [JsonPropertyName("classification")]
    public string Classification { get; set; }

    /// <summary>
    /// Hash of the previous audit entry (for hash chain integrity).
    /// </summary>
    [JsonPropertyName("previousHash")]
    public string PreviousHash { get; set; }

    /// <summary>
    /// SHA-256 hash of this entry (calculated from all fields + previousHash).
    /// </summary>
    [JsonPropertyName("currentHash")]
    public string CurrentHash { get; set; }

    /// <summary>
    /// Additional context or details about the event.
    /// </summary>
    [JsonPropertyName("details")]
    public string Details { get; set; }

    public AuditLog()
    {
        EventId = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
    }
    /// <summary>
    /// Basic Calculate the hash for this audit entry.
    /// </summary>
    public void CalculateHash()
    {
        string dataToHash = $"{EventId}|{Timestamp:O}|{ActorId}|{Action}|{ResourceId}|{Outcome}|{Classification}|{PreviousHash}|{Details}";
        // deterministic
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
            CurrentHash = Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Verify the integrity of this audit entry by recalculating its hash.
    /// </summary>
    public bool VerifyHash()
    {
        string originalHash = CurrentHash;
        CalculateHash();
        bool isValid = CurrentHash == originalHash;
        CurrentHash = originalHash; // Restore original hash
        return isValid;
    }
}




// government compliance audit log should follow-
// "NIST SP 800-53 Control AU-3 requirements". this need to cover
/**
    1.	What event occurred
    2.	When it occurred
    3.	Where it occurred
    4.	The source of the event
    5.	The outcome of the event
    6.	The identity of the user/process/system associated with the event (when applicable)
*/

