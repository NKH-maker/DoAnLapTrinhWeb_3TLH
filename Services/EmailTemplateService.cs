using System.Text;

namespace TINH_FINAL_2256.Services
{
    public interface IEmailTemplateService
    {
        string GetOrderConfirmationTemplate(string customerName, int orderId, decimal totalPrice, DateTime orderDate);
        string GetPasswordResetTemplate(string userName, string resetLink);
        string GetEmailConfirmationTemplate(string userName, string confirmLink);
        string GetOrderCancelledTemplate(string customerName, int orderId);
        string GetContactReplyTemplate(string customerName, string subject, string message);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private const string BrandColor = "#007bff";
        private const string DarkColor = "#343a40";

        public string GetOrderConfirmationTemplate(string customerName, int orderId, decimal totalPrice, DateTime orderDate)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, {BrandColor} 0%, #0056b3 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; background: white; }}
        .order-info {{ background: #f5f5f5; padding: 20px; border-left: 4px solid {BrandColor}; margin: 20px 0; border-radius: 4px; }}
        .order-info-row {{ display: flex; justify-content: space-between; margin: 10px 0; padding: 8px 0; border-bottom: 1px dotted #ddd; }}
        .order-info-row:last-child {{ border-bottom: none; }}
        .label {{ font-weight: 600; color: {DarkColor}; }}
        .value {{ color: #555; }}
        .total {{ font-size: 20px; font-weight: bold; color: {BrandColor}; }}
        .cta-button {{ display: inline-block; background: {BrandColor}; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: 600; }}
        .cta-button:hover {{ background: #0056b3; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
        .footer a {{ color: {BrandColor}; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? ??n hàng ???c xác nh?n</h1>
            <p>C?m ?n b?n ?ã mua hàng t?i 3TLH Phone!</p>
        </div>
        <div class='content'>
            <h2>Xin chào {customerName},</h2>
            <p>Chúng tôi ?ã nh?n ???c ??n hàng c?a b?n. D??i ?ây là chi ti?t ??n hàng:</p>
            
            <div class='order-info'>
                <div class='order-info-row'>
                    <span class='label'>Mã ??n hàng:</span>
                    <span class='value'>#{orderId}</span>
                </div>
                <div class='order-info-row'>
                    <span class='label'>Ngày ??t:</span>
                    <span class='value'>{orderDate:dd/MM/yyyy HH:mm}</span>
                </div>
                <div class='order-info-row'>
                    <span class='label'>T?ng ti?n:</span>
                    <span class='value total'>{totalPrice:C}</span>
                </div>
            </div>

            <p>??n hàng c?a b?n ?ang ???c x? lý. Chúng tôi s? c?p nh?t tr?ng thái v?n chuy?n s?m nh?t.</p>
            
            <a href='https://3tlhphone.com/ShoppingCart/OrderDetails/{orderId}' class='cta-button'>Xem chi ti?t ??n hàng</a>

            <p><strong>C?n h? tr??</strong></p>
            <p>N?u b?n có b?t k? câu h?i nào, vui lòng <a href='https://3tlhphone.com/Contact'>liên h? v?i chúng tôi</a> ho?c tr? l?i email này.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 3TLH Phone Shop. T?t c? quy?n ???c b?o l?u.</p>
            <p><a href='https://3tlhphone.com'>Trang ch?</a> | <a href='https://3tlhphone.com/Contact'>Liên h?</a></p>
        </div>
    </div>
</body>
</html>";
        }

        public string GetPasswordResetTemplate(string userName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a52 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; background: white; }}
        .alert {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .cta-button {{ display: inline-block; background: #ff6b6b; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: 600; }}
        .cta-button:hover {{ background: #ee5a52; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? ??t l?i m?t kh?u</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p>Chúng tôi nh?n ???c yêu c?u ??t l?i m?t kh?u cho tài kho?n c?a b?n.</p>
            
            <div class='alert'>
                <strong>? L?u ý b?o m?t:</strong> N?u b?n không yêu c?u ??t l?i m?t kh?u, vui lòng b? qua email này và ??m b?o tài kho?n c?a b?n an toàn.
            </div>

            <p>Nh?p vào nút d??i ?? ??t l?i m?t kh?u:</p>
            <a href='{resetLink}' class='cta-button'>??t l?i m?t kh?u</a>

            <p>Ho?c sao chép liên k?t này vào trình duy?t:</p>
            <p style='word-break: break-all; background: #f5f5f5; padding: 10px; border-radius: 4px; font-size: 12px;'>{resetLink}</p>

            <p><strong>Liên k?t s? h?t h?n trong 24 gi?.</strong></p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 3TLH Phone Shop. T?t c? quy?n ???c b?o l?u.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GetEmailConfirmationTemplate(string userName, string confirmLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; background: white; }}
        .cta-button {{ display: inline-block; background: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: 600; }}
        .cta-button:hover {{ background: #20c997; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? Xác nh?n email</h1>
        </div>
        <div class='content'>
            <h2>Chào m?ng {userName}! ??</h2>
            <p>C?m ?n b?n ?ã ??ng ký tài kho?n t?i 3TLH Phone.</p>
            <p>?? hoàn t?t quá trình ??ng ký, vui lòng xác nh?n email c?a b?n:</p>
            
            <a href='{confirmLink}' class='cta-button'>Xác nh?n email</a>

            <p>Ho?c sao chép liên k?t này vào trình duy?t:</p>
            <p style='word-break: break-all; background: #f5f5f5; padding: 10px; border-radius: 4px; font-size: 12px;'>{confirmLink}</p>

            <p>Sau khi xác nh?n, b?n có th? ??ng nh?p và mua s?m ngay!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 3TLH Phone Shop. T?t c? quy?n ???c b?o l?u.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GetOrderCancelledTemplate(string customerName, int orderId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #fd7e14 0%, #ffc107 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; background: white; }}
        .cta-button {{ display: inline-block; background: #fd7e14; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: 600; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? ??n hàng ?ã h?y</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {customerName},</h2>
            <p>??n hàng #{orderId} c?a b?n ?ã ???c h?y thành công.</p>
            
            <p>N?u b?n mu?n mua s?m thêm, vui lòng <a href='https://3tlhphone.com'>quay l?i c?a hàng</a>.</p>
            
            <p>C?n h? tr?? <a href='https://3tlhphone.com/Contact'>Liên h? chúng tôi</a></p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 3TLH Phone Shop. T?t c? quy?n ???c b?o l?u.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GetContactReplyTemplate(string customerName, string subject, string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #17a2b8 0%, #00bcd4 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; background: white; }}
        .message-box {{ background: #f5f5f5; padding: 20px; border-left: 4px solid #17a2b8; margin: 20px 0; border-radius: 4px; }}
        .footer {{ background: #f9f9f9; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? Chúng tôi ?ã nh?n ???c thông báo</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {customerName},</h2>
            <p>C?m ?n b?n ?ã liên h? v?i chúng tôi!</p>
            
            <div class='message-box'>
                <p><strong>Ch? ??:</strong> {subject}</p>
                <p><strong>N?i dung:</strong></p>
                <p>{message}</p>
            </div>

            <p>Chúng tôi s? ph?n h?i l?i b?n trong vòng 24 gi?. C?m ?n b?n ?ã ch? ??i!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 3TLH Phone Shop. T?t c? quy?n ???c b?o l?u.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
