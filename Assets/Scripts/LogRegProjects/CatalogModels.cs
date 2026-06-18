using System.Collections.Generic;

public class CatalogRootCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<CatalogChildCategory> Children { get; set; } = new();
}

public class CatalogChildCategory
{
    public int Id { get; set; }
    public int RootCategoryId { get; set; }
    public string Name { get; set; }
    public List<CatalogProduct> Products { get; set; } = new();
}

public class CatalogProduct
{
    public int Id { get; set; }
    public int ChildCategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImagePath { get; set; }
    public List<CatalogProductFile> Files { get; set; } = new();
}

public class CatalogProductFile
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
}
