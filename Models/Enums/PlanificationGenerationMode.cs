namespace CongoTravel.Models.Enums
{
    public enum PlanificationGenerationMode
    {
        SemaineCourante = 0,
        MoisCourant = 1,
        MoisProchain = 2,
        PeriodePersonnalisee = 3
    }

    public enum PlanificationGenerationItemStatut
    {
        Cree = 0,
        Ignore = 1,
        Echec = 2
    }
}
