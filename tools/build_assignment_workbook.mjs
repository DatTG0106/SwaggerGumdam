import fs from "node:fs/promises";
import { Workbook, SpreadsheetFile } from "@oai/artifact-tool";

const outputDir = "D:/PRN_232/outputs/01a0eab7-3c3c-79c1-9b9d-7651a6ec1cf4";
const rows = [
  ["Thành viên 1","Tài khoản","","Đăng ký","POST","/api/v1/auth/register","Public","Other"],
  ["Thành viên 1","Tài khoản","","Đăng nhập JWT","POST","/api/v1/auth/login","Public","Other"],
  ["Thành viên 1","Tài khoản","","Xem hồ sơ","GET","/api/v1/auth/me","Customer/Admin","Other"],
  ["Thành viên 1","Địa chỉ","Address","Danh sách địa chỉ","GET","/api/v1/addresses","Customer","Read"],
  ["Thành viên 1","Địa chỉ","Address","Chi tiết địa chỉ","GET","/api/v1/addresses/{id}","Customer","Read"],
  ["Thành viên 1","Địa chỉ","Address","Thêm địa chỉ","POST","/api/v1/addresses","Customer","Create"],
  ["Thành viên 1","Địa chỉ","Address","Sửa địa chỉ","PUT","/api/v1/addresses/{id}","Customer","Update"],
  ["Thành viên 1","Địa chỉ","Address","Xóa địa chỉ","DELETE","/api/v1/addresses/{id}","Customer","Delete"],
  ["Thành viên 2","Danh mục","Category","Danh sách danh mục","GET","/api/v1/categories","Public","Read"],
  ["Thành viên 2","Danh mục","Category","Chi tiết danh mục","GET","/api/v1/categories/{id}","Public","Read"],
  ["Thành viên 2","Danh mục","Category","Thêm danh mục","POST","/api/v1/categories","Admin","Create"],
  ["Thành viên 2","Danh mục","Category","Sửa danh mục","PUT","/api/v1/categories/{id}","Admin","Update"],
  ["Thành viên 2","Danh mục","Category","Ngừng danh mục","DELETE","/api/v1/categories/{id}","Admin","Delete"],
  ["Thành viên 2","Sản phẩm","Product","Danh sách sản phẩm","GET","/api/v1/products","Public","Read"],
  ["Thành viên 2","Sản phẩm","Product","Chi tiết sản phẩm","GET","/api/v1/products/{id}","Public","Read"],
  ["Thành viên 2","Sản phẩm","Product","Thêm sản phẩm","POST","/api/v1/products","Admin","Create"],
  ["Thành viên 2","Sản phẩm","Product","Sửa sản phẩm","PUT","/api/v1/products/{id}","Admin","Update"],
  ["Thành viên 2","Sản phẩm","Product","Ngừng bán sản phẩm","DELETE","/api/v1/products/{id}","Admin","Delete"],
  ["Thành viên 2","Tìm kiếm","","Lọc/sắp xếp OData","GET","/odata/Products","Public","Other"],
  ["Thành viên 3","SKU","Variant","Danh sách SKU theo sản phẩm","GET","/api/v1/products/{id}/variants","Public","Read"],
  ["Thành viên 3","SKU","Variant","Chi tiết SKU","GET","/api/v1/variants/{id}","Public","Read"],
  ["Thành viên 3","SKU","Variant","Thêm SKU","POST","/api/v1/variants","Admin","Create"],
  ["Thành viên 3","SKU","Variant","Sửa SKU","PUT","/api/v1/variants/{id}","Admin","Update"],
  ["Thành viên 3","SKU","Variant","Ngừng bán SKU","DELETE","/api/v1/variants/{id}","Admin","Delete"],
  ["Thành viên 3","Tồn kho","","Điều chỉnh tồn kho","POST","/api/v1/variants/{id}/stock-adjustments","Admin","Other"],
  ["Thành viên 4","Giỏ hàng","CartItem","Xem giỏ hàng","GET","/api/v1/cart/items","Customer","Read"],
  ["Thành viên 4","Giỏ hàng","CartItem","Chi tiết mục giỏ","GET","/api/v1/cart/items/{id}","Customer","Read"],
  ["Thành viên 4","Giỏ hàng","CartItem","Thêm mục giỏ","POST","/api/v1/cart/items","Customer","Create"],
  ["Thành viên 4","Giỏ hàng","CartItem","Sửa số lượng","PUT","/api/v1/cart/items/{id}","Customer","Update"],
  ["Thành viên 4","Giỏ hàng","CartItem","Xóa mục giỏ","DELETE","/api/v1/cart/items/{id}","Customer","Delete"],
  ["Thành viên 4","Đơn hàng","","Danh sách đơn","GET","/api/v1/orders","Customer/Admin","Other"],
  ["Thành viên 4","Đơn hàng","","Chi tiết đơn","GET","/api/v1/orders/{id}","Customer/Admin","Other"],
  ["Thành viên 4","Đơn hàng","","Đặt hàng và giữ tồn","POST","/api/v1/orders","Customer","Other"],
  ["Thành viên 4","Đơn hàng","","Hủy đơn chưa trả","POST","/api/v1/orders/{id}/cancel","Customer/Admin","Other"],
  ["Thành viên 4","Đơn hàng","","Chuyển giao/vận đơn","PUT","/api/v1/orders/{id}/status","Admin","Other"],
  ["Thành viên 5","Voucher","Voucher","Danh sách voucher","GET","/api/v1/vouchers","Admin","Read"],
  ["Thành viên 5","Voucher","Voucher","Chi tiết voucher","GET","/api/v1/vouchers/{id}","Admin","Read"],
  ["Thành viên 5","Voucher","Voucher","Thêm voucher","POST","/api/v1/vouchers","Admin","Create"],
  ["Thành viên 5","Voucher","Voucher","Sửa voucher","PUT","/api/v1/vouchers/{id}","Admin","Update"],
  ["Thành viên 5","Voucher","Voucher","Ngừng voucher","DELETE","/api/v1/vouchers/{id}","Admin","Delete"],
  ["Thành viên 5","Đánh giá","Review","Danh sách đánh giá","GET","/api/v1/products/{id}/reviews","Public","Read"],
  ["Thành viên 5","Đánh giá","Review","Chi tiết đánh giá","GET","/api/v1/reviews/{id}","Public","Read"],
  ["Thành viên 5","Đánh giá","Review","Thêm đánh giá sau mua","POST","/api/v1/reviews","Customer","Create"],
  ["Thành viên 5","Đánh giá","Review","Sửa đánh giá","PUT","/api/v1/reviews/{id}","Customer","Update"],
  ["Thành viên 5","Đánh giá","Review","Xóa đánh giá","DELETE","/api/v1/reviews/{id}","Owner/Admin","Delete"],
  ["Thành viên 5","Thanh toán","","Tạo Stripe Checkout Session","POST","/api/v1/payments/orders/{id}/session","Customer","Other"],
  ["Thành viên 5","Thanh toán","","Giả lập thành công/thất bại","POST","/api/v1/payments/mock/orders/{id}/complete","Customer/Dev","Other"],
  ["Thành viên 5","Thanh toán","","Xử lý Stripe webhook","POST","/api/v1/payments/stripe/webhook","Stripe","Other"],
];

const wb = Workbook.create();
const sum = wb.worksheets.add("Tổng kết");
const detail = wb.worksheets.add("Chức năng thành viên");
for (const s of [sum, detail]) { s.showGridLines = false; s.getRange("A1:H60").format.font = { name: "Arial", size: 10, color: "#182230" }; }
sum.tabColor = "#172B4D";
detail.tabColor = "#5A7196";
sum.getRange("A2").values = [["Gundam Shop API · Phân công nhóm"]];
sum.getRange("A2").format.font = { name: "Arial", size: 15, bold: true, color: "#172B4D" };
sum.getRange("A3:H3").format.borders = { preset: "doubleBottom", style: "thin", color: "#98A6B8" };
sum.getRange("A4:H4").values = [["Thành viên", "Bộ CRUD kiểm tra", "Create", "Read", "Update", "Delete", "Kết quả", "Tổng chức năng"]];
sum.getRange("A4:H4").format = { fill: "#172B4D", font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" }, rowHeight: 28, verticalAlignment: "center", horizontalAlignment: "center" };
const assigned = [
  ["Thành viên 1", "Address"], ["Thành viên 2", "Product"], ["Thành viên 3", "Variant"],
  ["Thành viên 4", "CartItem"], ["Thành viên 5", "Voucher"]
];
sum.getRange("A5:B9").values = assigned;
for (let r = 5; r <= 9; r++) {
  for (const [col, action] of [["C","Create"],["D","Read"],["E","Update"],["F","Delete"]])
    sum.getRange(`${col}${r}`).formulas = [[`=COUNTIFS('Chức năng thành viên'!$A$5:$A$${rows.length+4},$A${r},'Chức năng thành viên'!$C$5:$C$${rows.length+4},$B${r},'Chức năng thành viên'!$H$5:$H$${rows.length+4},"${action}")`]];
  sum.getRange(`G${r}`).formulas = [[`=IF(MIN(C${r}:F${r})>=1,"Đạt","Thiếu")`]];
  sum.getRange(`H${r}`).formulas = [[`=COUNTIFS('Chức năng thành viên'!$A$5:$A$${rows.length+4},$A${r})`]];
}
sum.getRange("A11").values = [["Tổng chức năng"]];
sum.getRange("H11").formulas = [["=SUM(H5:H9)"]];
sum.getRange("A13").values = [["Ghi chú: Mỗi người có ít nhất một bộ Create · Read · Update · Delete hoàn chỉnh. Tên thành viên là nhãn tạm."]];
sum.getRange("A13").format.font = { name: "Arial", size: 10, italic: true, color: "#526178" };
sum.getRange("A5:H9").format.rowHeight = 25;
sum.getRange("A5:H9").format.borders = { preset: "inside", style: "thin", color: "#E6EAF0" };
sum.getRange("G5:G9").conditionalFormats.add("containsText", { text: "Thiếu", format: { fill: "#FDE8E7", font: { color: "#B42318", bold: true } } });
sum.getRange("A:A").format.columnWidth = 20;
sum.getRange("B:B").format.columnWidth = 21;
sum.getRange("C:F").format.columnWidth = 11;
sum.getRange("G:G").format.columnWidth = 13;
sum.getRange("H:H").format.columnWidth = 20;

detail.getRange("A2").values = [["Danh sách chức năng và endpoint"]];
detail.getRange("A2").format.font = { name: "Arial", size: 15, bold: true, color: "#172B4D" };
detail.getRange("A3:H3").format.borders = { preset: "doubleBottom", style: "thin", color: "#98A6B8" };
detail.getRange("A4:H4").values = [["Thành viên", "Phân hệ", "Thực thể CRUD", "Tên chức năng", "Method", "Endpoint", "Quyền", "Thao tác"]];
detail.getRange(`A5:H${rows.length+4}`).values = rows;
detail.getRange(`A4:H${rows.length+4}`).format.rowHeight = 22;
detail.getRange("A4:H4").format = { fill: "#172B4D", font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" }, rowHeight: 28, verticalAlignment: "center", horizontalAlignment: "center" };
detail.getRange("A:A").format.columnWidth = 20;
detail.getRange("B:B").format.columnWidth = 17;
detail.getRange("C:C").format.columnWidth = 18;
detail.getRange("D:D").format.columnWidth = 31;
detail.getRange("E:E").format.columnWidth = 11;
detail.getRange("F:F").format.columnWidth = 61;
detail.getRange("G:G").format.columnWidth = 18;
detail.getRange("H:H").format.columnWidth = 14;
detail.freezePanes.freezeRows(4);
detail.tables.add(`A4:H${rows.length+4}`, true, "FunctionsTable");
wb.recalculate();
const check = await wb.inspect({ kind: "table", sheetId: "Tổng kết", range: "A4:H11", include: "values,formulas", tableMaxRows: 8, tableMaxCols: 8, maxChars: 3000 });
console.log(check.ndjson);
const errors = await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!", options: { useRegex: true, maxResults: 30 }, summary: "formula error scan" });
console.log(errors.ndjson);
const preview = await wb.render({ sheetName: "Tổng kết", range: "A1:H13", scale: 1.5, format: "png" });
await fs.mkdir(outputDir, { recursive: true });
await fs.writeFile(`${outputDir}/assignment-summary-preview.png`, new Uint8Array(await preview.arrayBuffer()));
const preview2 = await wb.render({ sheetName: "Chức năng thành viên", range: "A1:H18", scale: 1, format: "png" });
await fs.writeFile(`${outputDir}/assignment-detail-preview.png`, new Uint8Array(await preview2.arrayBuffer()));
const file = await SpreadsheetFile.exportXlsx(wb);
await file.save(`${outputDir}/GundamShop_PhanCong.xlsx`);
console.log(`Saved ${outputDir}/GundamShop_PhanCong.xlsx`);
