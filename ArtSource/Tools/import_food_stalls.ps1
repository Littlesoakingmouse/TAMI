# Chép sheet quầy + đồ ăn từ thư mục hoạ sĩ vào ArtSource/Sheets (tên chuẩn) rồi cắt vào Assets.
# Chạy lại mỗi khi hoạ sĩ cập nhật sheet, sau đó chạy menu "Tami/Cập nhật hình tạm và dữ liệu" trong Unity.
$src = 'E:\unity_pj\TAMIGAME\Quay hang + do an'
$sheets = Join-Path $PSScriptRoot '..\Sheets'
$tool = Join-Path $PSScriptRoot 'cut_auto.py'
New-Item -ItemType Directory -Force $sheets | Out-Null

# Tên file có dấu (rê) nên tìm bằng ký tự đại diện để không phụ thuộc bảng mã của PowerShell.
function Pick($dir, $pattern) { (Get-ChildItem -LiteralPath $dir | Where-Object { $_.Name -like $pattern } | Select-Object -First 1).Name }
$stalls = @{
    'banh_chung' = (Pick "$src\quayhang" 'r* (4).png');'tteokbokki' = 'tok.png'; 'banh_mi' = 'banhmi.png'; 'kimbap' = 'kimbap.png'
    'pho' = 'pho.png'; 'naengmyeon' = 'Naengmyeon.png'; 'ca_phe' = 'cafe.png'; 'bingsu' = 'bingxu.png'
    'bun_cha' = 'buncha.png'; 'samgyeopsal' = 'Samgyeopsal.png'; 'banh_xeo' = 'banhxeo.png'; 'pajeon' = 'Pajeon.png'
}
foreach ($id in $stalls.Keys) {
    $dst = Join-Path $sheets "stall_$id.png"
    Copy-Item -LiteralPath (Join-Path "$src\quayhang" $stalls[$id]) $dst -Force
    py -3 -I $tool stall $dst $id
}

$foods = [ordered]@{
    (Pick "$src\do an" 'r* (3).png') = 'banh_chung', 'tteokbokki', 'banh_mi'
    'kimbap-pho-milanh.png' = 'kimbap', 'pho', 'naengmyeon'
    'cafe-bingxu-buncha.png' = 'ca_phe', 'bingsu', 'bun_cha'
    'Samgyeopsal-banhxeo-Pajeon.png' = 'samgyeopsal', 'banh_xeo', 'pajeon'
}
foreach ($file in $foods.Keys) {
    $ids = $foods[$file]
    $dst = Join-Path $sheets ('food_' + ($ids -join '_') + '.png')
    Copy-Item -LiteralPath (Join-Path "$src\do an" $file) $dst -Force
    py -3 -I $tool food $dst @ids
}
