using Microsoft.AspNetCore.Mvc;

namespace snap_test.Form_Data
{
    /// <summary>
    /// Form data: multipart/form-data file uploads, with CRUD over the uploaded file records.
    /// </summary>
    [ApiController]
    [Route("api/formdata")]
    public class FormDataController : ControllerBase
    {
        // CREATE (POST) – Accept form-data
        /// <summary>Upload a file with an optional description.</summary>
        /// <param name="file">The file to upload (required). Only its name is stored.</param>
        /// <param name="description">Optional description.</param>
        /// <remarks>Content-Type: <c>multipart/form-data</c> with fields <c>file</c> and <c>description</c>.</remarks>
        /// <response code="201">File record created.</response>
        /// <response code="400">No file sent.</response>
        [HttpPost("upload")]
        public IActionResult Upload(IFormFile? file, [FromForm] string? description)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "File is required" });

                int newId = FormDataStore.Records.Max(r => r.Id) + 1;

                FormDataStore.Records.Add(new FileRecord
                {
                    Id = newId,
                    FileName = file.FileName,
                    Description = description ?? string.Empty
                });

                return StatusCode(201, new
                {
                    message = "File uploaded",
                    id = newId,
                    fileName = file.FileName,
                    description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // GET ALL
        /// <summary>List all uploaded file records.</summary>
        /// <response code="200">All records.</response>
        [HttpGet("all")]
        public IActionResult GetAll()
        {
            try
            {
                return Ok(FormDataStore.Records);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // GET BY ID
        /// <summary>Get a file record by ID.</summary>
        /// <param name="id">Record ID.</param>
        /// <response code="200">The record.</response>
        /// <response code="404">No record with that ID.</response>
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                var record = FormDataStore.Records.FirstOrDefault(r => r.Id == id);

                if (record == null)
                    return NotFound(new { message = "Record not found" });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // UPDATE – form-data again
        /// <summary>Update a record's file name and/or description.</summary>
        /// <param name="id">Record ID.</param>
        /// <param name="file">Optional replacement file; only its name is stored.</param>
        /// <param name="description">Optional new description; empty keeps the current one.</param>
        /// <remarks>Content-Type: <c>multipart/form-data</c>. Both fields are optional.</remarks>
        /// <response code="200">Record updated.</response>
        /// <response code="404">No record with that ID.</response>
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, IFormFile? file, [FromForm] string? description)
        {
            try
            {
                var record = FormDataStore.Records.FirstOrDefault(r => r.Id == id);

                if (record == null)
                    return NotFound(new { message = "Record not found" });

                if (file != null)
                    record.FileName = file.FileName;

                if (!string.IsNullOrEmpty(description))
                    record.Description = description;

                return Ok(new
                {
                    message = "Record updated",
                    record
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // DELETE
        /// <summary>Delete a file record.</summary>
        /// <param name="id">Record ID.</param>
        /// <response code="200">Record deleted.</response>
        /// <response code="404">No record with that ID.</response>
        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var record = FormDataStore.Records.FirstOrDefault(r => r.Id == id);

                if (record == null)
                    return NotFound(new { message = "Record not found" });

                FormDataStore.Records.Remove(record);

                return Ok(new { message = "Record deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
