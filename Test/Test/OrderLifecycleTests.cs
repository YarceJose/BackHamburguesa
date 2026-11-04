using Domain.Enums;
using Domain.Errors;
using Domain.Exceptions;
using static Test.TestData;

namespace Test;

public class OrderLifecycleTests
{
    [Fact]
    public void Cash_order_goes_straight_to_the_kitchen()
    {
        var order = CashOrder(cashAmount: 50000);

        Assert.Equal(OrderStatus.Received, order.Status);
        Assert.Equal(Now, order.ReceivedAt);
        Assert.Equal(38000, order.Total);
        Assert.Equal(50000, order.CashAmount);
    }

    [Fact]
    public void Cash_amount_lower_than_total_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => CashOrder(cashAmount: 20000));
        Assert.Equal(ErrorCodes.OrderCashTooLow, ex.Code);
    }

    [Fact]
    public void Transfer_order_waits_for_receipt_and_admin_approval()
    {
        var order = TransferOrder();
        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Null(order.ReceivedAt);

        order.AttachReceipt("receipts/a.jpg", Now.AddMinutes(3));
        Assert.Equal(OrderStatus.PaymentReview, order.Status);

        order.ApprovePayment(adminId: 1, Now.AddMinutes(5));
        Assert.Equal(OrderStatus.Received, order.Status);
        Assert.Equal(Now.AddMinutes(5), order.ReceivedAt);
    }

    [Fact]
    public void Rejected_receipt_lets_the_customer_upload_again()
    {
        var order = TransferOrder();
        order.AttachReceipt("receipts/a.jpg", Now);
        order.RejectPayment(1, "El valor no coincide", Now.AddMinutes(1));

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Equal("El valor no coincide", order.PaymentRejectReason);

        order.AttachReceipt("receipts/b.jpg", Now.AddMinutes(2));
        Assert.Equal(OrderStatus.PaymentReview, order.Status);
        Assert.Null(order.PaymentRejectReason);
    }

    [Fact]
    public void Full_kitchen_and_delivery_flow()
    {
        var order = CashOrder();
        order.StartPreparing(cookId: 7, Now.AddMinutes(1));
        order.MarkLeftKitchen(cookId: 7, Now.AddMinutes(20));
        order.MarkPickedUp(deliveryUserId: 8, Now.AddMinutes(22));
        order.MarkDelivered(Now.AddMinutes(40));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(7, order.CookId);
        Assert.Equal(8, order.DeliveryUserId);
    }

    [Fact]
    public void Delivery_cannot_take_an_order_still_in_the_kitchen()
    {
        var order = CashOrder();
        var ex = Assert.Throws<DomainException>(() => order.MarkPickedUp(8, Now));
        Assert.Equal(ErrorCodes.OrderInvalidTransition, ex.Code);
    }

    [Fact]
    public void Kitchen_cannot_receive_an_unpaid_transfer_order()
    {
        var order = TransferOrder();
        Assert.Throws<DomainException>(() => order.StartPreparing(7, Now));
    }

    [Fact]
    public void Cash_order_cannot_attach_a_receipt()
    {
        var ex = Assert.Throws<DomainException>(() => CashOrder().AttachReceipt("receipts/a.jpg", Now));
        Assert.Equal(ErrorCodes.OrderNotTransfer, ex.Code);
    }

    [Fact]
    public void Delivered_order_cannot_be_cancelled()
    {
        var order = CashOrder();
        order.MarkLeftKitchen(7, Now);
        order.MarkPickedUp(8, Now);
        order.MarkDelivered(Now);
        Assert.Throws<DomainException>(() => order.Cancel("x", Now));
    }

    [Fact]
    public void Empty_order_and_bad_quantities_are_rejected()
    {
        Assert.Equal(ErrorCodes.OrderInvalidQuantity, Assert.Throws<DomainException>(() => CashOrder(quantity: 0)).Code);
        Assert.Equal(ErrorCodes.OrderInvalidQuantity, Assert.Throws<DomainException>(() => CashOrder(quantity: 51)).Code);
    }
}
