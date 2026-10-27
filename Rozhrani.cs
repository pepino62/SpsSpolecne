using System;

namespace SpsSpolecne
{
    // Udaje modelu aplikace, se kterymi pracuji spolecne sluzby - modely (_D2rezervace, _D4zaloha ...) zustavaji v aplikacich,
    // protoze se lisi chovanim obrazovky (slevy, vypocet RP ...), sluzby potrebuji jen tato pole.

    /// <summary>
    /// rezervace (d2 / dz2) - udaje pro kontroly zalohy a ceny
    /// </summary>
    public interface IRezervace
    {
        string F201 { get; }
        DateTime? F213 { get; }     // datum evidence ceny (zaplaceno)
        string F217 { get; }
        string F218 { get; }        // fakturacni adresa
        string F304 { get; }        // adresa bydliste
    }

    /// <summary>
    /// zaloha rezervace (d4 / dz4)
    /// </summary>
    public interface IZaloha
    {
        string F401 { get; set; }
        string F201 { get; }
        DateTime? F402 { get; }     // splatnost
        DateTime? F403 { get; }     // datum uhrazeni
        decimal F404 { get; }
        decimal F405 { get; }
        string F406 { get; }
        string F407 { get; }
    }

    /// <summary>
    /// osoba rezervace (d6 / dz6)
    /// </summary>
    public interface IOsoba
    {
        string F601 { get; set; }
        string F201 { get; }
        string F602 { get; }
        string F603 { get; }
        DateTime? F604 { get; }
        string F605 { get; }
        string F606 { get; }
        string F607 { get; }
        decimal F608 { get; }
        decimal F609 { get; }
        decimal F610 { get; }
        decimal F611 { get; }
        decimal F612 { get; }
    }

    /// <summary>
    /// polozka ceny rezervace (d5 / dz5) - _1 = mimosezona
    /// </summary>
    public interface ICena
    {
        string F501 { get; set; }
        string F201 { get; }
        string FT501 { get; }
        string FT508 { get; }
        decimal F502 { get; }
        decimal F503 { get; }
        decimal F504 { get; }
        decimal F505 { get; }
        string F506 { get; }
        string F507 { get; }
        decimal F502_1 { get; }
        decimal F503_1 { get; }
        decimal F505_1 { get; }
        string F506_1 { get; }
        string F507_1 { get; }
    }
}
