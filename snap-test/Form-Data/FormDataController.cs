using Microsoft.AspNetCore.Mvc;

namespace snap_test.Form_Data
{
    [ApiController]
    [Route("api/formdata")]
    public class FormDataController : ControllerBase
    {
        // CREATE (POST) – Accept form-data
        [HttpPost("upload")]
        public IActionResult Upload([FromForm] IFormFile file, [FromForm] string description)
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
                    Description = description
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
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromForm] IFormFile file, [FromForm] string description)
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
