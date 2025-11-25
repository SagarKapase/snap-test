namespace snap_test.Form_Data
{
    public static class FormDataStore
    {
        public static List<FileRecord> Records = new()
    {
        new FileRecord { Id = 1, FileName = "john_resume.pdf", Description = "Resume of John" },
        new FileRecord { Id = 2, FileName = "maria_photo.png", Description = "Profile photo of Maria" }
    };
    }


}
