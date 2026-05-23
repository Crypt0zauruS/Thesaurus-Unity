// =============================================================================
// DedaleData.cs — Plan du dédale 31×31 (matrice immuable du thésaurus p.2)
// -----------------------------------------------------------------------------
// CODES DES CELLULES :
//   ' '  couloir           (traversable)
//   'O'  mur Ouvrable      (destructible avec un ouvreur)
//   'X'  mur non ouvrable  (indestructible)
//   'E'  cellule d'Enclos  (zone de départ du joueur)
//
// SYSTÈME DE COORDONNÉES :
//   tabDedale[ligne][col] avec :
//     - ligne = axe Z (ligne 0 = nord/Z=0, ligne 30 = sud/Z=30)
//     - col   = axe X (col 0 = ouest/X=0, col 30 = est/X=30)
//   Position monde du CENTRE d'une cellule : (col + 0.5, Y, ligne + 0.5)
// =============================================================================

public static class DedaleData
{
    public const int TAILLE = 31;

    // Matrice originale immuable
    public static readonly string[] TabDedale = new string[]
    {
        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", // ligne  0 (nord)
        "X              O              X", // ligne  1
        "X OOOOOO OOOOO O OOOOO OOOOOO X", // ligne  2
        "X        O   O O O   O        X", // ligne  3
        "XOOOOOOO OO OO O OO OO OOOOOOOX", // ligne  4
        "X                             X", // ligne  5
        "XOOO OOO O OOO O OOO O OOO OOOX", // ligne  6
        "X        O O O O O O O        X", // ligne  7
        "XOOO OOO O O O O O O O OOO OOOX", // ligne  8
        "X        O O O O O O O        X", // ligne  9
        "X OOOOOO O O O O O O O OOOOOO X", // ligne 10
        "X      O OOO O O O OOO O      X", // ligne 11
        "XOOOOO O               O OOOOOX", // ligne 12
        "X      O OOO XX XX OOO O      X", // ligne 13
        "X OOOOOO O O XEEEX O O OOOOOO X", // ligne 14
        "X        O O XEEEX O O        X", // ligne 15
        "XOOOOOOO O O XEEEX O O OOOOOOOX", // ligne 16
        "X        O O XXXXX O O        X", // ligne 17
        "X OOOOOO O O       O O OOOOOO X", // ligne 18
        "X O      O OOOO OOOO O      O X", // ligne 19
        "X O OOOO O    O O    O OOOO O X", // ligne 20
        "X O O    O O  O O  O O    O O X", // ligne 21
        "X O OOOO O O OO OO O O OOOO O X", // ligne 22
        "X      O O   O   O   O O      X", // ligne 23
        "X OOOO O O OOO O OOO O O OOOO X", // ligne 24
        "X      O   O       O   O      X", // ligne 25
        "X O OO O OOO OO OO OOO O OO O X", // ligne 26
        "X O O  O O   O   O   O O  O O X", // ligne 27
        "XOO OOOO O OOOOOOOOO O OOOO OOX", // ligne 28
        "X                             X", // ligne 29
        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"  // ligne 30 (sud)
    };

    /// <summary>Retourne le caractère à la position (ligne, col).</summary>
    public static char GetCell(int ligne, int col)
    {
        return TabDedale[ligne][col];
    }

    /// <summary>Vrai si la cellule est un mur (X ou O).</summary>
    public static bool EstMur(int ligne, int col)
    {
        char c = TabDedale[ligne][col];
        return c == 'X' || c == 'O';
    }

    /// <summary>Vrai si la cellule est traversable (couloir ou enclos).</summary>
    public static bool EstTraversable(int ligne, int col)
    {
        char c = TabDedale[ligne][col];
        return c == ' ' || c == 'E';
    }
}
