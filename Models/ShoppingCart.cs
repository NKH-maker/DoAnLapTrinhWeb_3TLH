namespace TINH_FINAL_2256.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new
        List<CartItem>();
        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId ==
            item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
        }
        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }
        public void IncreaseQuantity(int productId, int delta = 1)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return;
            item.Quantity = Math.Max(1, item.Quantity + Math.Max(1, delta));
        }

        public void DecreaseQuantity(int productId, int delta = 1)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return;

            var newQty = item.Quantity - Math.Max(1, delta);
            if (newQty <= 0)
            {
                RemoveItem(productId);
                return;
            }

            item.Quantity = newQty;
        }
    }
}
