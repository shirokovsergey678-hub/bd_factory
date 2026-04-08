using System;

[System.Serializable]
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsArchived { get; set; }
    public string Username { get; set; }

    public string CreatedAtFormatted => CreatedAt.ToString("dd.MM.yyyy");
}