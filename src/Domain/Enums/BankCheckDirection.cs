namespace ERPSystem.Domain.Enums;

/// <summary>
/// اتجاه الشيك: مستلم من عميل (أصل «برسم التحصيل») أم مصدر لصالح مورد (التزام «برسم السداد»).
/// </summary>
public enum BankCheckDirection
{
    /// <summary>مستلم من عميل — يمثل مبلغاً سيصله النشاط ويُدخل في «شيكات برسم التحصيل» (1102).</summary>
    ReceivedFromCustomer = 1,

    /// <summary>مصدر لصالح مورد — يمثل التزاماً مستقبلياً ويُدخل في «شيكات برسم السداد» (2300).</summary>
    IssuedToSupplier = 2
}