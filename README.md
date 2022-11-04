# FuryDB

Static structured database for Unity. I create this project inspired by [CastleDB](http://castledb.org/).

# How to use

First [Install FDB](./Doc/Install/README.md). Create your database class `DB.cs`:

```DB.cs
using FDB;
using Newtonsoft.Json;

[JsonConverter(typeof(DBConverter<DB>))]
[FuryDB("Assets/Resources/DB.json.txt", "Assets/Kinds.cs")]
public class DB
{
    
}
```

Create folder `Editor` and then create class `Editor/DBWindow.cs`

```Editor/DBWindow.cs
using UnityEditor;
using FDB.Editor;

public class DBWindow : DBInspector<DB>
{
    [MenuItem("Game/DB")]
    public static void Open()
    {
        var window = CreateWindow<DBWindow>();
        window.title = "DB";
        window.Show();
    }
}

```

Then open **Game -> DB** and look at window

![New window](./Doc/1.png)

Press **New model**. Now you have empty database

![New window](./Doc/2.png)

Now lets reach `DB.cs` with few types

```DB.cs
using FDB;
using Newtonsoft.Json;

[JsonConverter(typeof(DBConverter<DB>))]
[FuryDB("Assets/Resources/DB.json.txt", "Assets/Kinds.cs")]
public class DB
{
    public Index<UnitConfig> Units;
    public Index<WeaponConfig> Weapons;

    [GroupBy("Kind", @"(.+?)_")]
    public Index<TextConfig> Texts;
}

public class UnitConfig
{
    public Kind<UnitConfig> Kind;
    public Ref<TextConfig> Name;
    public int Str;
    public int Dex;
    public int Int;
    public int Chr;
    public Ref<WeaponConfig> Weapon;
}

public enum WeaponType
{
    Melee,
    Range
}

public class WeaponConfig
{
    public Kind<WeaponConfig> Kind;
    public Ref<TextConfig> Name;
    public WeaponType Type;
    public int Damage;
    public int DamageVar;
}

public class TextConfig
{
    public Kind<TextConfig> Kind;
    public string En;
    public string Ru;
}
```

And fill database with data. Don't forget press **Save**

![Units](./Doc/3.png)
![Weapons](./Doc/4.png)
![Text](./Doc/5.png)

Now you can load database in your code

```Boot.cs
var db = DBResolver.Load<DB>();
foreach (var unit in db.Units.All()) {
    Debug.Log(unit.Kind);
}
```

You also can access for db items using `Kinds.cs`:

```Boot.cs
var db = DBResolver.Load<DB>();
var rogue = db.Units.Get(Kinds.Units.rogue);
```

# Supported types

- bool
- int
- float
- string
- enum
- Color
- AnimationCurve
- List<>
- Ref<>
- AssetReference