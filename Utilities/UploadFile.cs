namespace CongoTravel.Utilities ;


public class UploadFile
{
    // Verifier qu'il s'agit bien d'un fichier au format image
    public static bool TestImage(IFormFile file)
    {
        if (file != null)
        {
            var extension = file.FileName.Substring(file.FileName.Length - 3).ToUpper();
            return (extension == "JPG" || extension == "PNG" || extension == "GIF" || extension == "BMP");
        }
        return false;
    }

    // Ecriture du fichier
    public static string EcritureFichier(IFormFile file)
    {
        string ecritureOk;
        try
        {
            var extension = file.FileName.Substring(file.FileName.Length - 3).ToUpper();

            //Generer un identifiant unique pour s'en servir comme nom du fichier et  d'eviter tout connflict lie au nom
            var nomUnique = Guid.NewGuid();

            var cheminFichier = Path.Combine("wwwroot", "Images", nomUnique.ToString() + "." + extension);

            using (var stream = new FileStream(cheminFichier, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            ecritureOk = cheminFichier;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return "";
        }
        return ecritureOk;
    }

    public static bool DetruireFichier(string cheminFichier)
    {
        try
        {
            if (File.Exists(cheminFichier))
            {
                File.Delete(cheminFichier);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
        return true;
    }
}
