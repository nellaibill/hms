using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Masters.Infrastructure.Configurations;

/// <summary>
/// Maps <see cref="District"/> to masters.districts and seeds each state/union territory's
/// major, long-established districts — a representative set for Patient Registration's
/// Address dropdown, not an exhaustive/current-as-of-today list (India's district count and
/// boundaries change fairly often, e.g. recent splits in Andhra Pradesh, Telangana, Assam,
/// Rajasthan). More districts can be added later the same way this list was built: append a
/// name to the relevant state's array below and generate a new migration — no admin CRUD
/// screen exists for this yet (see docs/DecisionLog.md).
/// </summary>
internal class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    private static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts");

        builder.HasKey(d => d.Id).HasName("pk_districts");
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(d => d.StateId).HasColumnName("state_id").IsRequired();

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.CreatedBy).HasColumnName("created_by");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasIndex(d => new { d.StateId, d.Name }).IsUnique().HasDatabaseName("ux_districts_state_id_name").HasFilter("is_deleted = false");
        builder.HasIndex(d => d.StateId).HasDatabaseName("ix_districts_state_id");

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(d => d.StateId)
            .HasConstraintName("fk_districts_state_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(BuildSeed());
    }

    private static IEnumerable<object> BuildSeed()
    {
        var stateIds = StateConfiguration.Seed.ToDictionary(s => s.Name, s => s.Id);
        var index = 0;

        foreach (var (stateName, districtNames) in DistrictsByState)
        {
            var stateId = stateIds[stateName];
            foreach (var districtName in districtNames)
            {
                index++;
                yield return new
                {
                    Id = DistrictId(index),
                    Name = districtName,
                    StateId = stateId,
                    CreatedAt = SeedCreatedAt,
                    CreatedBy = (Guid?)null,
                    UpdatedAt = (DateTime?)null,
                    UpdatedBy = (Guid?)null,
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null,
                    DeletedBy = (Guid?)null,
                };
            }
        }
    }

    private static Guid DistrictId(int index) => Guid.Parse($"019a0200-0000-7000-8000-{index:D12}");

    private static readonly (string State, string[] Districts)[] DistrictsByState =
    [
        ("Andhra Pradesh", ["Anantapur", "Chittoor", "East Godavari", "Guntur", "Krishna", "Kurnool", "Nellore", "Prakasam", "Srikakulam", "Visakhapatnam", "Vizianagaram", "West Godavari", "Kadapa"]),
        ("Arunachal Pradesh", ["Tawang", "West Kameng", "East Kameng", "Papum Pare", "Lower Subansiri", "Upper Subansiri", "West Siang", "East Siang", "Upper Siang", "Lohit", "Changlang", "Tirap", "Anjaw", "Kurung Kumey"]),
        ("Assam", ["Kamrup", "Kamrup Metropolitan", "Nagaon", "Sonitpur", "Dibrugarh", "Jorhat", "Sivasagar", "Tinsukia", "Cachar", "Barpeta", "Darrang", "Dhubri", "Goalpara", "Golaghat", "Karimganj", "Lakhimpur", "Nalbari", "Bongaigaon", "Karbi Anglong"]),
        ("Bihar", ["Patna", "Gaya", "Bhagalpur", "Muzaffarpur", "Darbhanga", "Purnia", "Nalanda", "Rohtas", "Saran", "Vaishali", "Begusarai", "Samastipur", "Munger", "Saharsa", "Katihar", "Madhubani", "Sitamarhi", "East Champaran", "West Champaran", "Siwan", "Gopalganj", "Bhojpur", "Buxar", "Kaimur", "Aurangabad", "Jehanabad", "Nawada", "Jamui", "Banka", "Khagaria", "Supaul", "Araria", "Kishanganj", "Madhepura"]),
        ("Chhattisgarh", ["Raipur", "Bilaspur", "Durg", "Rajnandgaon", "Raigarh", "Korba", "Bastar", "Dhamtari", "Mahasamund", "Kanker", "Kabirdham", "Janjgir-Champa", "Koriya", "Surguja", "Dantewada", "Jashpur"]),
        ("Goa", ["North Goa", "South Goa"]),
        ("Gujarat", ["Ahmedabad", "Surat", "Vadodara", "Rajkot", "Bhavnagar", "Jamnagar", "Junagadh", "Gandhinagar", "Anand", "Bharuch", "Mehsana", "Kutch", "Navsari", "Valsad", "Panchmahal", "Sabarkantha", "Banaskantha", "Kheda", "Patan", "Porbandar", "Amreli", "Surendranagar", "Dahod", "Narmada", "Tapi", "Botad"]),
        ("Haryana", ["Ambala", "Bhiwani", "Faridabad", "Fatehabad", "Gurugram", "Hisar", "Jhajjar", "Jind", "Kaithal", "Karnal", "Kurukshetra", "Mahendragarh", "Nuh", "Palwal", "Panchkula", "Panipat", "Rewari", "Rohtak", "Sirsa", "Sonipat", "Yamunanagar"]),
        ("Himachal Pradesh", ["Bilaspur", "Chamba", "Hamirpur", "Kangra", "Kinnaur", "Kullu", "Lahaul and Spiti", "Mandi", "Shimla", "Sirmaur", "Solan", "Una"]),
        ("Jharkhand", ["Ranchi", "East Singhbhum", "Dhanbad", "Bokaro", "Deoghar", "Giridih", "Hazaribagh", "Koderma", "Palamu", "Garhwa", "Chatra", "Gumla", "Simdega", "Lohardaga", "Pakur", "Sahibganj", "Godda", "Dumka", "Jamtara", "West Singhbhum", "Ramgarh", "Khunti", "Latehar", "Seraikela Kharsawan"]),
        ("Karnataka", ["Bengaluru Urban", "Bengaluru Rural", "Mysuru", "Belagavi", "Ballari", "Bidar", "Vijayapura", "Kalaburagi", "Dakshina Kannada", "Udupi", "Uttara Kannada", "Shivamogga", "Chikkamagaluru", "Hassan", "Kodagu", "Mandya", "Chamarajanagar", "Tumakuru", "Kolar", "Chikkaballapur", "Ramanagara", "Davanagere", "Chitradurga", "Dharwad", "Gadag", "Haveri", "Koppal", "Raichur", "Yadgir"]),
        ("Kerala", ["Thiruvananthapuram", "Kollam", "Pathanamthitta", "Alappuzha", "Kottayam", "Idukki", "Ernakulam", "Thrissur", "Palakkad", "Malappuram", "Kozhikode", "Wayanad", "Kannur", "Kasaragod"]),
        ("Madhya Pradesh", ["Bhopal", "Indore", "Jabalpur", "Gwalior", "Ujjain", "Sagar", "Satna", "Rewa", "Ratlam", "Dewas", "Vidisha", "Chhindwara", "Betul", "Khandwa", "Khargone", "Dhar", "Mandsaur", "Neemuch", "Sehore", "Raisen", "Damoh", "Panna", "Chhatarpur", "Tikamgarh", "Guna", "Shivpuri", "Morena", "Bhind", "Balaghat", "Seoni", "Mandla", "Shahdol", "Sidhi", "Singrauli", "Katni", "Jhabua", "Barwani", "Rajgarh"]),
        ("Maharashtra", ["Mumbai City", "Mumbai Suburban", "Thane", "Palghar", "Raigad", "Ratnagiri", "Sindhudurg", "Pune", "Satara", "Sangli", "Solapur", "Kolhapur", "Nashik", "Dhule", "Nandurbar", "Jalgaon", "Ahmednagar", "Chhatrapati Sambhajinagar", "Jalna", "Beed", "Latur", "Dharashiv", "Nanded", "Parbhani", "Hingoli", "Amravati", "Akola", "Washim", "Buldhana", "Yavatmal", "Wardha", "Nagpur", "Bhandara", "Gondia", "Chandrapur", "Gadchiroli"]),
        ("Manipur", ["Imphal East", "Imphal West", "Bishnupur", "Thoubal", "Churachandpur", "Chandel", "Senapati", "Tamenglong", "Ukhrul"]),
        ("Meghalaya", ["East Khasi Hills", "West Khasi Hills", "Jaintia Hills", "Ri Bhoi", "East Garo Hills", "West Garo Hills", "South Garo Hills"]),
        ("Mizoram", ["Aizawl", "Lunglei", "Champhai", "Mamit", "Kolasib", "Serchhip", "Lawngtlai", "Saiha"]),
        ("Nagaland", ["Kohima", "Dimapur", "Mokokchung", "Tuensang", "Wokha", "Zunheboto", "Phek", "Mon", "Peren", "Kiphire", "Longleng"]),
        ("Odisha", ["Khordha", "Cuttack", "Puri", "Ganjam", "Balasore", "Mayurbhanj", "Sundargarh", "Sambalpur", "Bolangir", "Kalahandi", "Koraput", "Bhadrak", "Jagatsinghpur", "Kendrapara", "Jajpur", "Dhenkanal", "Angul", "Nayagarh", "Gajapati", "Rayagada", "Nabarangpur", "Nuapada", "Bargarh", "Jharsuguda", "Boudh", "Kandhamal", "Malkangiri", "Keonjhar"]),
        ("Punjab", ["Amritsar", "Barnala", "Bathinda", "Faridkot", "Fatehgarh Sahib", "Fazilka", "Ferozepur", "Gurdaspur", "Hoshiarpur", "Jalandhar", "Kapurthala", "Ludhiana", "Mansa", "Moga", "Sri Muktsar Sahib", "Pathankot", "Patiala", "Rupnagar", "Sangrur", "SAS Nagar", "Shaheed Bhagat Singh Nagar", "Tarn Taran"]),
        ("Rajasthan", ["Jaipur", "Jodhpur", "Udaipur", "Kota", "Ajmer", "Bikaner", "Alwar", "Bharatpur", "Bhilwara", "Sikar", "Pali", "Sri Ganganagar", "Hanumangarh", "Churu", "Jhunjhunu", "Nagaur", "Barmer", "Jaisalmer", "Jalore", "Sirohi", "Dungarpur", "Banswara", "Chittorgarh", "Rajsamand", "Bundi", "Baran", "Jhalawar", "Karauli", "Dholpur", "Sawai Madhopur", "Tonk", "Dausa", "Pratapgarh"]),
        ("Sikkim", ["East Sikkim", "West Sikkim", "North Sikkim", "South Sikkim"]),
        ("Tamil Nadu", ["Chennai", "Coimbatore", "Madurai", "Tiruchirappalli", "Salem", "Tirunelveli", "Erode", "Vellore", "Thoothukudi", "Dindigul", "Thanjavur", "Kanchipuram", "Tiruvallur", "Cuddalore", "Villupuram", "Namakkal", "Karur", "Krishnagiri", "Dharmapuri", "Nagapattinam", "Nilgiris", "Pudukkottai", "Ramanathapuram", "Sivaganga", "Theni", "Tiruvannamalai", "Virudhunagar", "Ariyalur", "Perambalur", "Kanyakumari", "Tiruppur", "Tenkasi"]),
        ("Telangana", ["Hyderabad", "Rangareddy", "Medak", "Nizamabad", "Adilabad", "Karimnagar", "Warangal", "Khammam", "Nalgonda", "Mahbubnagar"]),
        ("Tripura", ["West Tripura", "South Tripura", "North Tripura", "Dhalai", "Gomati", "Sepahijala", "Khowai", "Unakoti"]),
        ("Uttar Pradesh", ["Lucknow", "Kanpur Nagar", "Agra", "Varanasi", "Meerut", "Prayagraj", "Ghaziabad", "Bareilly", "Aligarh", "Moradabad", "Saharanpur", "Gorakhpur", "Gautam Buddha Nagar", "Firozabad", "Jhansi", "Muzaffarnagar", "Mathura", "Rampur", "Shahjahanpur", "Farrukhabad", "Mau", "Hardoi", "Fatehpur", "Raebareli", "Sitapur", "Bahraich", "Basti", "Ballia", "Azamgarh", "Ayodhya", "Sultanpur", "Pratapgarh", "Jaunpur", "Ghazipur", "Deoria", "Kushinagar", "Etawah", "Budaun", "Bulandshahr", "Barabanki"]),
        ("Uttarakhand", ["Dehradun", "Haridwar", "Nainital", "Udham Singh Nagar", "Almora", "Pithoragarh", "Bageshwar", "Champawat", "Chamoli", "Rudraprayag", "Tehri Garhwal", "Pauri Garhwal", "Uttarkashi"]),
        ("West Bengal", ["Kolkata", "Howrah", "North 24 Parganas", "South 24 Parganas", "Hooghly", "Nadia", "Murshidabad", "Malda", "Uttar Dinajpur", "Dakshin Dinajpur", "Jalpaiguri", "Darjeeling", "Cooch Behar", "Alipurduar", "Purba Bardhaman", "Paschim Bardhaman", "Purba Medinipur", "Paschim Medinipur", "Bankura", "Purulia", "Birbhum", "Kalimpong", "Jhargram"]),
        ("Andaman and Nicobar Islands", ["South Andaman", "North and Middle Andaman", "Nicobar"]),
        ("Chandigarh", ["Chandigarh"]),
        ("Dadra and Nagar Haveli and Daman and Diu", ["Dadra and Nagar Haveli", "Daman", "Diu"]),
        ("Delhi", ["New Delhi", "North Delhi", "South Delhi", "East Delhi", "West Delhi", "Central Delhi", "North East Delhi", "North West Delhi", "South East Delhi", "South West Delhi", "Shahdara"]),
        ("Jammu and Kashmir", ["Srinagar", "Jammu", "Anantnag", "Baramulla", "Budgam", "Pulwama", "Kupwara", "Kulgam", "Shopian", "Bandipora", "Ganderbal", "Udhampur", "Kathua", "Samba", "Rajouri", "Poonch", "Doda", "Ramban", "Kishtwar", "Reasi"]),
        ("Ladakh", ["Leh", "Kargil"]),
        ("Lakshadweep", ["Lakshadweep"]),
        ("Puducherry", ["Puducherry", "Karaikal", "Mahe", "Yanam"]),
    ];
}
