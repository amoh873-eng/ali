using ERPSystem.Application.DTOs.Reports;
using Microsoft.EntityFrameworkCore;
namespace ERPSystem.Application.Services;
public partial class ReportService
{
    private async Task<GenericReportTable> GetAging(string key, DateTime asOf, Func<string,List<string>,List<List<string>>,List<string>?,string?,bool,GenericReportTable> R)
    {
        if(key=="ar-aging"){
            var invs=await _context.Set<ERPSystem.Domain.Entities.SalesInvoice>().Where(s=>!s.IsDeleted && s.Status!=ERPSystem.Domain.Enums.DocumentStatus.Cancelled).ToListAsync();
            var rows=invs.Where(s=>s.TotalAmount - s.PaidAmount > 0.01m).Select(s=>{var bal=s.TotalAmount-s.PaidAmount; var due=(s.DueDate??s.InvoiceDate).Date; var days=(asOf.Date-due).Days; string b=days<=0?"حالي":days<=30?"1-30":days<=60?"31-60":days<=90?"61-90":"90+"; return new List<string>{s.InvoiceNumber, due.ToString("yyyy-MM-dd"), days.ToString(), b, bal.ToString("N2")};}).ToList();
            return R("أعمار الذمم - عملاء",new(){ "الفاتورة","الاستحقاق","أيام","الفئة","الرصيد"},rows,null,null,true);
        } else {
            var invs=await _context.Set<ERPSystem.Domain.Entities.PurchaseInvoice>().Where(s=>!s.IsDeleted && s.Status!=ERPSystem.Domain.Enums.DocumentStatus.Cancelled).ToListAsync();
            var rows=invs.Where(s=>s.TotalAmount - s.PaidAmount > 0.01m).Select(s=>{var bal=s.TotalAmount-s.PaidAmount; var due=(s.DueDate??s.InvoiceDate).Date; var days=(asOf.Date-due).Days; string b=days<=0?"حالي":days<=30?"1-30":days<=60?"31-60":days<=90?"61-90":"90+"; return new List<string>{s.InvoiceNumber, due.ToString("yyyy-MM-dd"), days.ToString(), b, bal.ToString("N2")};}).ToList();
            return R("أعمار الذمم - موردون",new(){ "الفاتورة","الاستحقاق","أيام","الفئة","الرصيد"},rows,null,null,true);
        }
    }
    private async Task<GenericReportTable> GetReportAsyncB(string key,Dictionary<string,string?> pars,DateTime from,DateTime to,DateTime asOf,Func<string,List<string>,List<List<string>>,List<string>?,string?,bool,GenericReportTable> R)
    {
        if(key=="ar-aging" || key=="ap-aging") return await GetAging(key,asOf,R);
        switch(key)
        {
            case "sales-by-item": { var ls=await _context.Set<ERPSystem.Domain.Entities.SalesInvoiceLine>().Include(l=>l.Item).Where(l=>l.SalesInvoice!=null && !l.SalesInvoice.IsDeleted && l.SalesInvoice.InvoiceDate>=from.Date && l.SalesInvoice.InvoiceDate<to.Date.AddDays(1)).ToListAsync(); var g=ls.GroupBy(l=>l.Item?.NameAr??l.ItemId.ToString()).Select(x=>new{N=x.Key,Qty=x.Sum(v=>v.Quantity),Tot=x.Sum(v=>v.LineTotal)}).OrderByDescending(x=>x.Tot).ToList(); var rows=g.Select(x=>new List<string>{x.N,x.Qty.ToString("N2"),x.Tot.ToString("N2")}).ToList(); return R("المبيعات حسب الصنف",new(){ "الصنف","الكمية","الإجمالي"},rows,null,null,true); }
            case "stock-valuation": { var items=await _context.Set<ERPSystem.Domain.Entities.Item>().Include(i=>i.Category).Where(i=>!i.IsDeleted).ToListAsync(); var rows=items.Select(i=>new List<string>{i.Code,i.NameAr,i.CurrentStock.ToString("N2"),i.CostPrice.ToString("N2"),(i.CurrentStock*i.CostPrice).ToString("N2"),i.Category?.NameAr??""}).ToList(); var tot=items.Sum(i=>i.CurrentStock*i.CostPrice); return R("تقييم المخزون",new(){ "الرمز","الصنف","الرصيد","التكلفة","القيمة","الفئة"},rows,new(){ "","","","الإجمالي",tot.ToString("N2"),""},"طابق مع رصيد حساب المخزون",true); }
            case "low-stock": { var items=await _context.Set<ERPSystem.Domain.Entities.Item>().Where(i=>!i.IsDeleted && i.CurrentStock < i.MinStockLevel).ToListAsync(); var rows=items.Select(i=>new List<string>{i.Code,i.NameAr,i.CurrentStock.ToString("N2"),i.MinStockLevel.ToString("N2"),(i.MinStockLevel-i.CurrentStock).ToString("N2")}).ToList(); return R("تنبيه نقص المخزون",new(){ "الرمز","الصنف","الرصيد","الحد","العجز"},rows,null,null,true); }
            case "employee-directory": { var emps=await _context.Set<ERPSystem.Domain.Entities.Employee>().Where(e=>!e.IsDeleted).Include(e=>e.Department).ToListAsync(); var rows=emps.Select(e=>new List<string>{e.EmployeeNumber,e.NameAr,e.Department?.NameAr??"",e.Status.ToString()}).ToList(); return R("دليل الموظفين",new(){ "الرقم","الاسم","القسم","الحالة"},rows,null,null,true); }
            case "expenses-by-category": { var exps=await _context.Set<ERPSystem.Domain.Entities.ExpenseEntry>().Include(e=>e.ExpenseCategory).Where(e=>!e.IsDeleted && e.ExpenseDate>=from.Date && e.ExpenseDate<to.Date.AddDays(1)).ToListAsync(); var g=exps.GroupBy(e=>e.ExpenseCategory?.NameAr??"غير مصنف").Select(x=>new{C=x.Key,Tot=x.Sum(v=>v.Amount)}).ToList(); var rows=g.Select(x=>new List<string>{x.C,x.Tot.ToString("N2")}).ToList(); return R("المصاريف حسب الفئة",new(){ "الفئة","الإجمالي"},rows,null,null,true); }
            case "pos-daily": { var invs=await _context.Set<ERPSystem.Domain.Entities.SalesInvoice>().Where(s=>s.IsPos && !s.IsDeleted && s.InvoiceDate.Date==asOf.Date).ToListAsync(); var rows=new List<List<string>>{ new(){ "عدد العمليات",invs.Count.ToString()},new(){ "الإجمالي",invs.Sum(s=>s.TotalAmount).ToString("N2")}}; return R("ملخص المبيعات اليومي",new(){ "البند","القيمة"},rows,null,null,true); }
            case "leads-pipeline": { var leads=await _context.Set<ERPSystem.Domain.Entities.Lead>().Where(l=>!l.IsDeleted).ToListAsync(); var g=leads.GroupBy(l=>l.Status.ToString()).Select(x=>new List<string>{x.Key,x.Count().ToString()}).ToList(); return R("مسار العملاء المحتملين",new(){ "المرحلة","العدد"},g,null,null,true); }
            case "kpi-summary": { var sales=await _context.Set<ERPSystem.Domain.Entities.SalesInvoice>().Where(s=>!s.IsDeleted && s.InvoiceDate>=from.Date && s.InvoiceDate<to.Date.AddDays(1)).ToListAsync(); var prevFrom=from.AddDays(-(to-from).TotalDays); var prev=await _context.Set<ERPSystem.Domain.Entities.SalesInvoice>().Where(s=>!s.IsDeleted && s.InvoiceDate>=prevFrom.Date && s.InvoiceDate<from.Date).ToListAsync(); decimal cur=sales.Sum(s=>s.TotalAmount), prv=prev.Sum(s=>s.TotalAmount); var rows=new List<List<string>>{ new(){ "إيراد الفترة",cur.ToString("N2")},new(){ "السابقة",prv.ToString("N2")},new(){ "التغير %",prv==0?"—":((cur-prv)*100/prv).ToString("N1")+"%"}}; return R("ملخص المؤشرات",new(){ "المؤشر","القيمة"},rows,null,"تجميع سريع",true); }
            default: throw new InvalidOperationException($"Unknown report: {key}");
        }
    }
}
