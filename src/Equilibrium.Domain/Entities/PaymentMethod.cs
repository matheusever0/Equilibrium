﻿using Equilibrium.Domain.Entities;
using Equilibrium.Domain.Enums;

public class PaymentMethod
{
    public Guid Id { get; protected set; }
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public bool IsSystem { get; protected set; }
    public PaymentMethodType Type { get; protected set; }
    public Guid? UserId { get; protected set; }
    public User User { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public ICollection<Payment> Payments { get; protected set; }
    public ICollection<CreditCard> CreditCards { get; protected set; }

    protected PaymentMethod()
    {
        Payments = [];
        CreditCards = [];
    }

    public PaymentMethod(string name, string description, PaymentMethodType type)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Type = type;
        IsSystem = true;
        CreatedAt = DateTime.Now;
        Payments = [];
        CreditCards = [];
    }

    public PaymentMethod(string name, string description, PaymentMethodType type, User user)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Type = type;
        IsSystem = false;
        UserId = user.Id;
        User = user;
        CreatedAt = DateTime.Now;
        Payments = [];
        CreditCards = [];
    }

    public void UpdateName(string name)
    {
        if (IsSystem)
            throw new InvalidOperationException("Cannot update system payment method");

        Name = name;
    }

    public void UpdateDescription(string description)
    {
        if (IsSystem)
            throw new InvalidOperationException("Cannot update system payment method");

        Description = description;
    }
}