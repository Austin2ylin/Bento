using Bento.Api.Models;
using FluentValidation;

namespace Bento.Api.Validators;

public class OrderValidator : AbstractValidator<CreateOrderRequest>
{
    public OrderValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("使用者編號必須大於 0。");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("至少需要一個訂單品項。")
            .Must(items => items.Select(i => i.MenuItemId).Distinct().Count() == items.Count)
            .WithMessage("同一個菜單品項請合併成一筆。")
            .Must(items => items.Sum(i => i.Quantity) <= 50)
            .WithMessage("單筆訂單總數量不可超過 50。")
            .When(x => x.Items is not null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId)
                .GreaterThan(0)
                .WithMessage("菜單編號必須大於 0。");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("數量至少為 1。")
                .LessThanOrEqualTo(20)
                .WithMessage("單一品項數量不可超過 20。")
                .When(i => i is not null);
        });
    }
}
