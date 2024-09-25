﻿using MedicExpermed.Models;
using MedicExpermed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using System.Globalization;


namespace MedicExpermed.Controllers
{
    public class ConsultationController : Controller
    {
        private readonly AppointmentService _citaService;
        private readonly PatientService _patientService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConsultationService _consultationService;
        private readonly CatalogService _catalogService;
        private readonly ILogger<ConsultationController> _logger;
        private readonly medicossystembdIContext _medical_SystemContext;

        public ConsultationController(AppointmentService citaService, PatientService patientService, IHttpContextAccessor httpContextAccessor, ConsultationService consultationService, CatalogService catalogService, ILogger<ConsultationController> logger, medicossystembdIContext medical_SystemContext)
        {
            _citaService = citaService;
            _patientService = patientService;
            _httpContextAccessor = httpContextAccessor;
            _consultationService = consultationService;
            _catalogService = catalogService;
            _logger = logger;
            _medical_SystemContext = medical_SystemContext;
        }



        private async Task CargarListasDesplegables()
        {
            ViewBag.TiposDocumentos = await _catalogService.ObtenerTiposDocumentoAsync();
            ViewBag.TiposSangre = await _catalogService.ObtenerTiposSangreAsync();
            ViewBag.TiposGenero = await _catalogService.ObtenerTiposGeneroAsync();
            ViewBag.TiposEstadoCivil = await _catalogService.ObtenerTiposEstadoCivilAsync();
            ViewBag.TiposFormacion = await _catalogService.ObtenerTiposFormacionAsync();
            ViewBag.TiposNacionalidad = await _catalogService.ObtenerTiposDeNacionalidadPAsync();
            ViewBag.TiposProvincia = await _catalogService.ObtenerTiposDeProvinciaPAsync();
            ViewBag.TiposSeguro = await _catalogService.ObtenerTiposSeguroSaludAsync();
            ViewBag.TiposPariente = await _catalogService.ObtenerTiposParentescoAsync();
            ViewBag.TiposAlergias = await _catalogService.ObtenerAlergiasAsync();
            ViewBag.TiposCirugias = await _catalogService.ObtenerCirugiasAsync();
            ViewBag.TiposParienteAntece = await _catalogService.ObtenerAntecedentesFAsync();
            ViewBag.TiposMedicamentos = await _catalogService.ObtenerMedicamentosActivasAsync();
            ViewBag.TiposLaboratorios = await _catalogService.ObtenerLaboratorioActivasAsync();
            ViewBag.TiposImagen = await _catalogService.ObtenerImagenActivasAsync();
            ViewBag.TiposDiagnostico = await _catalogService.ObtenerDiagnosticosActivasAsync();
        }

        [HttpGet("Listar-Consultas")]
        public async Task<IActionResult> ListarConsultas()
        {
            try
            {
                var consultas = await _consultationService.GetAllConsultasAsync();
                ViewBag.UsuarioEspecialidad = HttpContext.Session.GetString("UsuarioEspecialidad");

                return View(consultas);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<Consultum>());
            }
        }

        [HttpGet("Crear-Consulta")]
        public async Task<IActionResult> CrearConsulta()
        {
            // Obtener IDs de "NINGUNA"
            int ningunTipoParienteId = await _catalogService.ObtenerIdParentescoNingunoAsync();
            int ningunAlergiasId = await _catalogService.ObtenerIdAlergiasNingunoAsync();

            // Crear una nueva instancia de Consultation
            var consulta = new Consultation
            {
                TipoPariente = ningunTipoParienteId,
                FechaCreacion = DateTime.Now
            };

            // Añadir el ID "NINGUNA" a la lista de alergias
            consulta.Alergias.Add(new ConsultaAlergiaDTO
            {
                CatalogoalergiaId = ningunAlergiasId,
                ObservacionAlergias = "Ninguna alergia registrada",
                EstadoAlergias = 1
            });

            // Asignar datos a ViewBag desde la sesión
            ViewBag.UsuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.UsuarioId = HttpContext.Session.GetInt32("UsuarioId");
            ViewBag.UsuarioIdEspecialidad = HttpContext.Session.GetInt32("UsuarioIdEspecialidad");

            // Cargar listas desplegables
            await CargarListasDesplegables();

            // Devolver el modelo a la vista
            return View(consulta);
        }


        [HttpPost("Crear-Consulta")]
        public async Task<IActionResult> CrearConsulta([FromBody] Consultation consultaDto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                foreach (var error in errores)
                {
                    Console.WriteLine($"Campo: {error.Field}, Errores: {string.Join(", ", error.Errors)}");
                }

                return Json(new { success = false, errores });
            }



            try
            {
                // Llama al servicio para crear la consulta
                await _consultationService.CrearConsultaAsync(
                    consultaDto.UsuarioCreacion,
                    consultaDto.HistorialConsulta,
                    consultaDto.PacienteId,
                    consultaDto.MotivoConsulta,
                    consultaDto.EnfermedadConsulta,
                    consultaDto.NombrePariente,
                    consultaDto.SignosAlarma,
                    consultaDto.ReconocimientoFarmacologico,
                    consultaDto.TipoPariente,
                    consultaDto.TelefonoPariente,
                    consultaDto.Temperatura,
                    consultaDto.FrecuenciaRespiratoria,
                    consultaDto.PresionArterialSistolica,
                    consultaDto.PresionArterialDiastolica,
                    consultaDto.Pulso,
                    consultaDto.Peso,
                    consultaDto.Talla,
                    consultaDto.PlanTratamiento,
                    consultaDto.Observacion,
                    consultaDto.AntecedentesPersonales,
                    consultaDto.DiasIncapacidad,
                    consultaDto.MedicoId,
                    consultaDto.EspecialidadId,
                    consultaDto.TipoConsultaId ?? 0,
                    consultaDto.NotasEvolucion,
                    consultaDto.ConsultaPrincipal,
                    consultaDto.EstadoConsulta,
                    // Parámetros de órganos y sistemas
                    consultaDto.OrganosSistemas.OrgSentidos,
                    consultaDto.OrganosSistemas.ObserOrgSentidos,
                    consultaDto.OrganosSistemas.Respiratorio,
                    consultaDto.OrganosSistemas.ObserRespiratorio,
                    consultaDto.OrganosSistemas.CardioVascular,
                    consultaDto.OrganosSistemas.ObserCardioVascular,
                    consultaDto.OrganosSistemas.Digestivo,
                    consultaDto.OrganosSistemas.ObserDigestivo,
                    consultaDto.OrganosSistemas.Genital,
                    consultaDto.OrganosSistemas.ObserGenital,
                    consultaDto.OrganosSistemas.Urinario,
                    consultaDto.OrganosSistemas.ObserUrinario,
                    consultaDto.OrganosSistemas.MEsqueletico,
                    consultaDto.OrganosSistemas.ObserMEsqueletico,
                    consultaDto.OrganosSistemas.Endocrino,
                    consultaDto.OrganosSistemas.ObserEndocrino,
                    consultaDto.OrganosSistemas.Linfatico,
                    consultaDto.OrganosSistemas.ObserLinfatico,
                    consultaDto.OrganosSistemas.Nervioso,
                    consultaDto.OrganosSistemas.ObserNervioso,
                    // Parámetros de examen físico
                    consultaDto.ExamenFisico.Cabeza,
                    consultaDto.ExamenFisico.ObserCabeza,
                    consultaDto.ExamenFisico.Cuello,
                    consultaDto.ExamenFisico.ObserCuello,
                    consultaDto.ExamenFisico.Torax,
                    consultaDto.ExamenFisico.ObserTorax,
                    consultaDto.ExamenFisico.Abdomen,
                    consultaDto.ExamenFisico.ObserAbdomen,
                    consultaDto.ExamenFisico.Pelvis,
                    consultaDto.ExamenFisico.ObserPelvis,
                    consultaDto.ExamenFisico.Extremidades,
                    consultaDto.ExamenFisico.ObserExtremidades,
                    // Parámetros de antecedentes familiares
                    consultaDto.AntecedentesFamiliares.Cardiopatia,
                    consultaDto.AntecedentesFamiliares.ObserCardiopatia,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoCardiopatia,
                    consultaDto.AntecedentesFamiliares.Diabetes,
                    consultaDto.AntecedentesFamiliares.ObserDiabetes,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoDiabetes,
                    consultaDto.AntecedentesFamiliares.EnfCardiovascular,
                    consultaDto.AntecedentesFamiliares.ObserEnfCardiovascular,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoEnfcardiovascular,
                    consultaDto.AntecedentesFamiliares.Hipertension,
                    consultaDto.AntecedentesFamiliares.ObserHipertension,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoHipertension,
                    consultaDto.AntecedentesFamiliares.Cancer,
                    consultaDto.AntecedentesFamiliares.ObserCancer,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoCancer,
                    consultaDto.AntecedentesFamiliares.Tuberculosis,
                    consultaDto.AntecedentesFamiliares.ObserTuberculosis,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoTuberculosis,
                    consultaDto.AntecedentesFamiliares.EnfMental,
                    consultaDto.AntecedentesFamiliares.ObserEnfMental,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoEnfmental,
                    consultaDto.AntecedentesFamiliares.EnfInfecciosa,
                    consultaDto.AntecedentesFamiliares.ObserEnfInfecciosa,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoEnfinfecciosa,
                    consultaDto.AntecedentesFamiliares.MalFormacion,
                    consultaDto.AntecedentesFamiliares.ObserMalFormacion,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoMalformacion,
                    consultaDto.AntecedentesFamiliares.Otro,
                    consultaDto.AntecedentesFamiliares.ObserOtro,
                    consultaDto.AntecedentesFamiliares.ParentescocatalogoOtro,
                    // Tablas relacionadas
                    consultaDto.Alergias,
                    consultaDto.Cirugias,
                    consultaDto.Medicamentos,
                    consultaDto.Laboratorios,
                    consultaDto.Imagenes,
                    consultaDto.Diagnosticos
                );

                // Devuelve un JSON con éxito si es una solicitud AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    TempData["SuccessMessage"] = $"La consulta creado exitosamente";

                    return Json(new { success = true, message = "Consulta creada exitosamente" });

                }
                else
                {
                    // Redirigir en caso de una solicitud normal (no AJAX)
                    TempData["SuccessMessage"] = "Consulta Registrado.";
                    return Json(new { success = true, redirectUrl = Url.Action("ListarConsultas") });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear el paciente: {ex.Message}";

                _logger.LogError(ex, "Error al crear la consulta");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Ocurrió un error en el servidor.", details = ex.Message });
                }
                else
                {
                    return View("Error", new ErrorViewModel { RequestId = ex.Message });
                }
            }
        }


        // VER CONSULTA

        [HttpGet("Ver-Consulta/{id}")]
        public async Task<IActionResult> VerConsulta(int id)
        {
            // Obtener la consulta existente por su ID desde el servicio que ya mapea los datos
            var consulta = await _consultationService.ObtenerConsultaPorIdAsync(id);

            if (consulta == null)
            {
                return NotFound(); // Maneja el caso en que no se encuentra la consulta
            }

            // Cargar datos adicionales para la vista
            ViewBag.UsuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.UsuarioId = HttpContext.Session.GetInt32("UsuarioId");
            ViewBag.UsuarioIdEspecialidad = HttpContext.Session.GetInt32("UsuarioIdEspecialidad");

            await CargarListasDesplegables(); // Carga los dropdowns si es necesario

            // Devuelve directamente el modelo mapeado a la vista
            return View(consulta);
        }




        //EDITAR CONSULTA
        [HttpGet("Editar-Consulta")]
        public async Task<IActionResult> EditarConsulta()
        {
            int ningunTipoParienteId = await _catalogService.ObtenerIdParentescoNingunoAsync();

            ViewBag.UsuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
            ViewBag.UsuarioId = HttpContext.Session.GetInt32("UsuarioId");
            ViewBag.UsuarioIdEspecialidad = HttpContext.Session.GetInt32("UsuarioIdEspecialidad");

            await CargarListasDesplegables();

            var model = new Consultation
            {
                TipoPariente = ningunTipoParienteId,
                FechaCreacion = DateTime.Now,
            };



            return View(model);
        }



        // Endpoint para actualizar una consulta existente
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConsultation(
            int id,
            [FromBody] Consultation request)
        {
            if (id != request.Id)
            {
                return BadRequest("El ID de la consulta no coincide con el ID en el cuerpo de la solicitud.");
            }

            try
            {
                await _consultationService.ActualizarConsultaAsync(
                    request.Id,
                    request.UsuarioCreacion,
                    request.HistorialConsulta,
                    request.PacienteId,
                    request.MotivoConsulta,
                    request.EnfermedadConsulta,
                    request.NombrePariente,
                    request.SignosAlarma,
                    request.ReconocimientoFarmacologico,
                    request.TipoPariente,
                    request.TelefonoPariente,
                    request.Temperatura,
                    request.FrecuenciaRespiratoria,
                    request.PresionArterialSistolica,
                    request.PresionArterialDiastolica,
                    request.Pulso,
                    request.Peso,
                    request.Talla,
                    request.PlanTratamiento,
                    request.Observacion,
                    request.AntecedentesPersonales,
                    request.DiasIncapacidad,
                    request.MedicoId,
                    request.EspecialidadId,
                    request.TipoConsultaId ?? 0,
                    request.NotasEvolucion,
                    request.ConsultaPrincipal,
                    request.EstadoConsulta,
// Parámetros para órganos y sistemas con manejo de booleanos nullable
request.OrganosSistemas.OrgSentidos ?? false,        // Manejo de bool?
request.OrganosSistemas.ObserOrgSentidos,
request.OrganosSistemas.Respiratorio ?? false,       // Manejo de bool?
request.OrganosSistemas.ObserRespiratorio,
request.OrganosSistemas.CardioVascular ?? false,     // Manejo de bool?
request.OrganosSistemas.ObserCardioVascular,
request.OrganosSistemas.Digestivo ?? false,          // Manejo de bool?
request.OrganosSistemas.ObserDigestivo,
request.OrganosSistemas.Genital ?? false,            // Manejo de bool?
request.OrganosSistemas.ObserGenital,
request.OrganosSistemas.Urinario ?? false,           // Manejo de bool?
request.OrganosSistemas.ObserUrinario,
request.OrganosSistemas.MEsqueletico ?? false,       // Manejo de bool?
request.OrganosSistemas.ObserMEsqueletico,
request.OrganosSistemas.Endocrino ?? false,          // Manejo de bool?
request.OrganosSistemas.ObserEndocrino,
request.OrganosSistemas.Linfatico ?? false,          // Manejo de bool?
request.OrganosSistemas.ObserLinfatico,
request.OrganosSistemas.Nervioso ?? false,           // Manejo de bool?
request.OrganosSistemas.ObserNervioso,


// Parámetros para examen físico con manejo de booleanos nullable
request.ExamenFisico.Cabeza ?? false, // Manejo de bool?
request.ExamenFisico.ObserCabeza,
request.ExamenFisico.Cuello ?? false, // Manejo de bool?
request.ExamenFisico.ObserCuello,
request.ExamenFisico.Torax ?? false, // Manejo de bool?
request.ExamenFisico.ObserTorax,
request.ExamenFisico.Abdomen ?? false, // Manejo de bool?
request.ExamenFisico.ObserAbdomen,
request.ExamenFisico.Pelvis ?? false, // Manejo de bool?
request.ExamenFisico.ObserPelvis,
request.ExamenFisico.Extremidades ?? false, // Manejo de bool?
request.ExamenFisico.ObserExtremidades
,
// Parámetros para antecedentes familiares, manejo de nulos con valor predeterminado
request.AntecedentesFamiliares.Cardiopatia ?? false,
request.AntecedentesFamiliares.ObserCardiopatia,
request.AntecedentesFamiliares.ParentescocatalogoCardiopatia ?? default(int),
request.AntecedentesFamiliares.Diabetes ?? false,
request.AntecedentesFamiliares.ObserDiabetes,
request.AntecedentesFamiliares.ParentescocatalogoDiabetes ?? default(int),
request.AntecedentesFamiliares.EnfCardiovascular ?? false,
request.AntecedentesFamiliares.ObserEnfCardiovascular,
request.AntecedentesFamiliares.ParentescocatalogoEnfcardiovascular ?? default(int),
request.AntecedentesFamiliares.Hipertension ?? false,
request.AntecedentesFamiliares.ObserHipertension,
request.AntecedentesFamiliares.ParentescocatalogoHipertension ?? default(int),
request.AntecedentesFamiliares.Cancer ?? false,
request.AntecedentesFamiliares.ObserCancer,
request.AntecedentesFamiliares.ParentescocatalogoCancer ?? default(int),
request.AntecedentesFamiliares.Tuberculosis ?? false,
request.AntecedentesFamiliares.ObserTuberculosis,
request.AntecedentesFamiliares.ParentescocatalogoTuberculosis ?? default(int),
request.AntecedentesFamiliares.EnfMental ?? false,
request.AntecedentesFamiliares.ObserEnfMental,
request.AntecedentesFamiliares.ParentescocatalogoEnfmental ?? default(int),
request.AntecedentesFamiliares.EnfInfecciosa ?? false,
request.AntecedentesFamiliares.ObserEnfInfecciosa,
request.AntecedentesFamiliares.ParentescocatalogoEnfinfecciosa ?? default(int),
request.AntecedentesFamiliares.MalFormacion ?? false,
request.AntecedentesFamiliares.ObserMalFormacion,
request.AntecedentesFamiliares.ParentescocatalogoMalformacion ?? default(int),
request.AntecedentesFamiliares.Otro ?? false,
request.AntecedentesFamiliares.ObserOtro,
request.AntecedentesFamiliares.ParentescocatalogoOtro ?? default(int),
                    // Tablas relacionadas
                    request.Alergias,
                    request.Cirugias,
                    request.Medicamentos,
                    request.Laboratorios,
                    request.Imagenes,
                    request.Diagnosticos
                );

                return Ok(new { Message = "Consulta actualizada exitosamente." });
            }
            catch (Exception ex)
            {
                // Manejo de excepciones (puedes hacer logging aquí si es necesario)
                return StatusCode(500, new { Message = "Ocurrió un error al actualizar la consulta.", Details = ex.Message });
            }
        }



        [HttpPost]
        public async Task<IActionResult> GeneratePdf(int id, string tipoDocumento)
        {
            // Espera a que se complete la tarea asincrónica para obtener la consulta
            var consulta = await _consultationService.GetConsultByIdWithAllergiesAsync(id);

            if (consulta == null)
            {
                return NotFound();
            }

            // Generar el PDF según el tipo de documento
            var document = GenerateDocumentByType(tipoDocumento, consulta);

            using var memoryStream = new MemoryStream();
            document.GeneratePdf(memoryStream);

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/pdf", $"Consulta_{id}_{tipoDocumento}.pdf");
        }


        private IDocument GenerateDocumentByType(string tipoDocumento, ConsultDetails consulta)
        {
            // Configuración para cada tipo de documento
            switch (tipoDocumento)
            {
                case "receta":
                    return CreateRecetaDocument(consulta);
                case "justificacion":
                    return CreateJustificacionDocument(consulta);
                case "consulta":
                    return CreateConsultaDocument(consulta);
                case "laboratorio":
                    return CreateLaboratorioDocument(consulta);
                case "imagen":
                    return CreateImagenDocument(consulta);
                default:
                    throw new InvalidOperationException("Tipo de documento no soportado.");
            }
        }


        private IDocument CreateRecetaDocument(ConsultDetails consulta)
        {
            // Tamaño de página A5 con márgenes grandes
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Size(1194, 847); // Tamaño A3 horizontal

                    page.DefaultTextStyle(x => x.FontFamily("Arial")); // Cambiar la familia de fuentes a Arial

                    page.Header().Row(row =>
                    {
                        row.ConstantItem(550).Column(col =>
                        {
                            col.Item().Text("Dr:" + consulta.NombresMedico + consulta.ApellidosMedico).Bold().FontSize(14); // Encabezado más grande
                            col.Item().Text("Especialista en: " + consulta.NombreEspecialidad).FontSize(12);
                            col.Item().Text("e-mail: " + consulta.EmailMedico).FontSize(11);
                            col.Item().Text("Teléfonos: " + consulta.TelefonoMedico).FontSize(11);
                            col.Item().PaddingTop(2).Text("").FontSize(11);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("Dr:" + consulta.NombresMedico + consulta.ApellidosMedico).Bold().FontSize(14); // Encabezado más grande
                            col.Item().Text("Especialista en: " + consulta.NombreEspecialidad).FontSize(12);
                            col.Item().Text("e-mail: " + consulta.EmailMedico).FontSize(11);
                            col.Item().Text("Teléfonos: " + consulta.TelefonoMedico).FontSize(11);
                            col.Item().PaddingTop(2).Text("").FontSize(11);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });
                    });


                    page.Content().Column(content =>
                    {
                        // Primera fila
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(400).Column(col =>
                            {

                                col.Item().PaddingTop(10)
                                    .Text(text =>
                                    {
                                        text.Span("Fecha: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.FechaCreacion.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                    });


                                col.Item().PaddingTop(2)
                                     .Text(text =>
                                      {
                                          text.Span("Apellidos: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                          text.Span(consulta.PrimerApellidoPaciente + " " + consulta.SegundoApellidoPaciente).FontSize(11); // Valor de la fecha sin negrita
                                      });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Nombres: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.PrimerNombrePaciente + " " + consulta.SegundoNombrePaciente).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Edad: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.EdadPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                col.Item().PaddingTop(2)
       .Text(text =>
       {
           text.Span("Alergias: ").FontSize(11).Bold();

           var alergias = consulta.Alergias.Select(a => a.NombreAlergia.ToLower()).ToList(); // Convertir todas a minúsculas
           string alergiasEnOracion;

           if (alergias.Count == 1)
           {
               alergiasEnOracion = alergias[0]; // Solo una alergia
           }
           else if (alergias.Count == 2)
           {
               alergiasEnOracion = string.Join(" , ", alergias); // Dos alergias, unir con "y"
           }
           else
           {
               alergiasEnOracion = string.Join(", ", alergias.Take(alergias.Count - 1)) +
                                   " , " + alergias.Last(); // Varias alergias, con "y" antes de la última
           }

           alergiasEnOracion = char.ToUpper(alergiasEnOracion[0]) + alergiasEnOracion.Substring(1); // Capitalizar la primera letra

           text.Span(alergiasEnOracion).FontSize(11); // Mostrar las alergias en oración
       });


                                // Información de alergias

                                // Información de diagnósticos
                                col.Item().Text(text =>
                                {
                                    text.Span("Diagnóstico: ").FontSize(11).Bold();

                                    var diagnosticos = consulta.Diagnosticos.Select(d => d.NombreDiagnostico.ToLower()).ToList(); // Convertir a minúsculas
                                    string diagnosticosEnOracion;

                                    if (diagnosticos.Count == 1)
                                    {
                                        diagnosticosEnOracion = diagnosticos[0]; // Solo un diagnóstico
                                    }
                                    else if (diagnosticos.Count == 2)
                                    {
                                        diagnosticosEnOracion = string.Join(" , ", diagnosticos); // Dos diagnósticos, unir con "y"
                                    }
                                    else
                                    {
                                        diagnosticosEnOracion = string.Join(", ", diagnosticos.Take(diagnosticos.Count - 1)) +
                                                                " , " + diagnosticos.Last(); // Varios diagnósticos, "y" antes del último
                                    }

                                    diagnosticosEnOracion = char.ToUpper(diagnosticosEnOracion[0]) + diagnosticosEnOracion.Substring(1); // Capitalizar la primera letra

                                    text.Span(diagnosticosEnOracion).FontSize(11); // Mostrar la oración con los diagnósticos
                                });


                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                // Muestra todos los secuenciales de los medicamentos
                                if (consulta.Medicamentos.Any())
                                {
                                    col.Item().PaddingTop(2)
                                   .Text(text =>
                                   {
                                       text.Span("Receta: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                       text.Span(consulta.Medicamentos.First().SecuencialMedicamento.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                   });
                                }
                                else
                                {
                                    col.Item().PaddingTop(10).Text("Secuencial: No hay medicamentos").Bold().FontSize(14);
                                }

                                col.Item().PaddingTop(2)
                               .Text(text =>
                               {
                                   text.Span("CC: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                   text.Span(consulta.CiPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                               });
                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(400).Column(col =>
                            {

                                col.Item().PaddingTop(10)
                                    .Text(text =>
                                    {
                                        text.Span("Fecha: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.FechaCreacion.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                    });


                                col.Item().PaddingTop(2)
                                     .Text(text =>
                                     {
                                         text.Span("Apellidos: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                         text.Span(consulta.PrimerApellidoPaciente + " " + consulta.SegundoApellidoPaciente).FontSize(11); // Valor de la fecha sin negrita
                                     });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Nombres: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.PrimerNombrePaciente + " " + consulta.SegundoNombrePaciente).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Edad: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.EdadPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                col.Item().PaddingTop(2)
       .Text(text =>
       {
           text.Span("Alergias: ").FontSize(11).Bold();

           var alergias = consulta.Alergias.Select(a => a.NombreAlergia.ToLower()).ToList(); // Convertir todas a minúsculas
           string alergiasEnOracion;

           if (alergias.Count == 1)
           {
               alergiasEnOracion = alergias[0]; // Solo una alergia
           }
           else if (alergias.Count == 2)
           {
               alergiasEnOracion = string.Join(" , ", alergias); // Dos alergias, unir con "y"
           }
           else
           {
               alergiasEnOracion = string.Join(", ", alergias.Take(alergias.Count - 1)) +
                                   " , " + alergias.Last(); // Varias alergias, con "y" antes de la última
           }

           alergiasEnOracion = char.ToUpper(alergiasEnOracion[0]) + alergiasEnOracion.Substring(1); // Capitalizar la primera letra

           text.Span(alergiasEnOracion).FontSize(11); // Mostrar las alergias en oración
       });


                                // Información de alergias

                                // Información de diagnósticos
                                col.Item().Text(text =>
                                {
                                    text.Span("Diagnóstico: ").FontSize(11).Bold();

                                    var diagnosticos = consulta.Diagnosticos.Select(d => d.NombreDiagnostico.ToLower()).ToList(); // Convertir a minúsculas
                                    string diagnosticosEnOracion;

                                    if (diagnosticos.Count == 1)
                                    {
                                        diagnosticosEnOracion = diagnosticos[0]; // Solo un diagnóstico
                                    }
                                    else if (diagnosticos.Count == 2)
                                    {
                                        diagnosticosEnOracion = string.Join(" , ", diagnosticos); // Dos diagnósticos, unir con "y"
                                    }
                                    else
                                    {
                                        diagnosticosEnOracion = string.Join(", ", diagnosticos.Take(diagnosticos.Count - 1)) +
                                                                " , " + diagnosticos.Last(); // Varios diagnósticos, "y" antes del último
                                    }

                                    diagnosticosEnOracion = char.ToUpper(diagnosticosEnOracion[0]) + diagnosticosEnOracion.Substring(1); // Capitalizar la primera letra

                                    text.Span(diagnosticosEnOracion).FontSize(11); // Mostrar la oración con los diagnósticos
                                });


                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                // Muestra todos los secuenciales de los medicamentos
                                if (consulta.Medicamentos.Any())
                                {
                                    col.Item().PaddingTop(2)
                                   .Text(text =>
                                   {
                                       text.Span("Receta: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                       text.Span(consulta.Medicamentos.First().SecuencialMedicamento.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                   });
                                }
                                else
                                {
                                    col.Item().PaddingTop(10).Text("Secuencial: No hay medicamentos").Bold().FontSize(14);
                                }
                                col.Item().PaddingTop(2)
                                .Text(text =>
                                {
                                    text.Span("CC: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                    text.Span(consulta.CiPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                });
                            });


                        });

                        // Segunda fila (Nueva fila)
                        // Segunda fila (Nueva fila)
                        // Segunda fila (Nueva fila)
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(550).PaddingTop(50).Column(col =>
                            {
                                col.Item().PaddingBottom(6).Text("PRESCRIPCIÓN").Bold().FontSize(14); // Título
                                                                                                      // Recorre la lista de medicamentos y muestra su información
                                foreach (var medicamento in consulta.Medicamentos)
                                {
                                    // Crear una fila para cada medicamento
                                    col.Item().Row(innerRow =>
                                    {
                                        innerRow.ConstantItem(400).Column(innerCol =>
                                        {
                                            innerCol.Item().Text("("+medicamento.CIE10MED+")"+ medicamento.NombreMedicamento).FontSize(12);
                                        });

                                        innerRow.ConstantItem(40).Column(innerCol => // Ajusta el tamaño de esta columna para dar más espacio
                                        {
                                            innerCol.Item().Text("x" + medicamento.DosisMedicamento).FontSize(12); // Cantidad de medicamento
                                        });
                                    });
                                }
                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(550).PaddingTop(50).Column(col =>
                            {
                                col.Item().PaddingBottom(7).Text("INDICACIONES").Bold().FontSize(14); // Título
                                                                                                      // Recorre la lista de medicamentos y muestra las observaciones
                                foreach (var medicamento in consulta.Medicamentos)
                                {
                                    col.Item().MinWidth(300).Text("(" + medicamento.CIE10MED + ")" + medicamento.NombreMedicamento + medicamento.ObserMedicamento).FontSize(12);
                                }

                                // Si hay espacio adicional, puedes agregar más celdas vacías si es necesario
                                for (int i = consulta.Medicamentos.Count; i < 6; i++) // Por ejemplo, llenar hasta 12 celdas
                                {
                                    col.Item().MinWidth(300).Text("").FontSize(12);
                                }
                            });
                        });




                        // Tercera fila (Nueva fila)

                        content.Item().Row(row =>
                        {
                            row.ConstantItem(550).PaddingTop(20).Column(col =>
                            {

                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(550).PaddingTop(200).Column(col =>
                            {
                                col.Item().Text("Recomendaciones no Farmacologicas").Bold().FontSize(14); // Nuevo contenido
                                col.Item().Text(consulta.ReconoFarmacologicas).FontSize(12);
                                col.Item().Text("").FontSize(12);
                                col.Item().Text("").FontSize(12);
                                col.Item().Text("").FontSize(12);
                                col.Item().Text("").FontSize(12);
                                col.Item().Text("").FontSize(12);
                                col.Item().PaddingTop(30).Text("").FontSize(12);




                            });


                        });



                    });

                    page.Footer().Column(footer =>
                    {// Nueva fila del footer
                        footer.Item().Row(row =>
                        {
                            row.ConstantItem(300).Column(col =>
                            {
                            });
                            row.ConstantItem(230).Border(1).Column(col =>
                            {
                                col.Item().Text("Dispensada").FontSize(11).AlignStart();
                                col.Item().Text("").FontSize(11).AlignStart();
                                col.Item().Text("Completamente:......... Parcialmente:.........").FontSize(11).AlignStart();
                                col.Item().Text("").FontSize(11).AlignStart();

                                col.Item().Text("Proxima Cita").FontSize(11).AlignStart();
                                col.Item().Text("_____________").FontSize(11).AlignCenter();


                            });

                            row.AutoItem().PaddingHorizontal(30).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(300).Column(col =>
                            {
                            });
                            row.ConstantItem(230).Border(1).Column(col =>
                            {
                                col.Item().Text("Dispensada").FontSize(11).AlignStart();
                                col.Item().Text("").FontSize(11).AlignStart();
                                col.Item().Text("Completamente:......... Parcialmente:.........").FontSize(11).AlignStart();
                                col.Item().Text("").FontSize(11).AlignStart();

                                col.Item().Text("Proxima Cita").FontSize(11).AlignStart();
                                col.Item().Text("_____________").FontSize(11).AlignCenter();


                            });
                        });

                        // Primera fila del footer
                        footer.Item().Row(row =>
                        {
                            row.ConstantItem(530).Column(col =>
                            {
                                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().Text(consulta.DireccionMedico).FontSize(11).AlignCenter();
                            });

                            row.AutoItem().PaddingHorizontal(30).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().Text(consulta.DireccionMedico).FontSize(11).AlignCenter();
                            });
                        });


                    });

                });

            });
        }

        private IDocument CreateJustificacionDocument(ConsultDetails consulta)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Tamaño de la página en formato A4 (595 x 842 puntos)
                    page.Size(595, 842);

                    // Márgenes uniformes en los bordes
                    page.Margin(40);

                    // Encabezado del documento con información del médico y la clínica
                    page.Header().Column(column =>
                    {
                        column.Spacing(5); // Espaciado entre los elementos del encabezado

                        // Información de la clínica o el hospital
                        //column.Item().Text(consulta.MedicoConsultaDNavigation.DireccionUsuario).FontSize(18).Bold().AlignCenter();
                        // Fecha de emisión del justificante en formato largo

                        // Información del médico
                        column.Item().Text($"Dr. {consulta.NombresMedico} {consulta.ApellidosMedico}").FontSize(8).AlignCenter();
                        column.Item().Text($"Colegiado No. {consulta.CodigoMedico}").FontSize(9).AlignCenter();
                        column.Item().Text($"Especialista en {consulta.NombreEspecialidad}").FontSize(9).AlignCenter();

                        // Dirección, teléfono y correo electrónico de la clínica
                        column.Item().Text(consulta.DireccionMedico).FontSize(9).AlignCenter();
                        column.Item().Text($"Teléfono: {consulta.TelefonoMedico}").FontSize(9).AlignCenter();
                        column.Item().Text($"Correo electrónico: {consulta.EmailMedico}").FontSize(9).AlignCenter();
                    });

                    // Contenido del justificante médico
                    page.Content().PaddingVertical(30).Column(column =>
                    {
                        column.Spacing(15); // Espaciado entre los elementos
                        column.Item().Text($"Quito, {DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", new CultureInfo("es-ES"))}").FontSize(9).AlignStart();

                        // Título del justificante con estilo
                        column.Item().Text("JUSTIFICANTE MÉDICO").FontSize(20).Bold().Underline().AlignCenter();


                        // Cuerpo del justificante
                        column.Item().Text($"Por medio de la presente, se hace constar que el/la paciente {consulta.PrimerNombrePaciente} {consulta.PrimerApellidoPaciente}, con documento de identidad {consulta.CiPacientes}, ha sido atendido/a en esta consulta el día {consulta.FechaCreacion}, debido a {consulta.MotivoConsulta}.").FontSize(11).WrapAnywhere();

                        column.Item().Text(consulta.FechaCreacion.HasValue
                            ? $"Se le ha indicado {consulta.DiasIncapacidad} días de incapacidad, por lo cual se recomienda que se abstenga de asistir a sus actividades desde {consulta.FechaCreacion.Value.ToString("dd/MM/yyyy")} hasta {consulta.FechaCreacion.Value.AddDays(consulta.DiasIncapacidad).ToString("dd/MM/yyyy")} durante dicho período."
                            : "La fecha de creación de la consulta no está disponible.", TextStyle.Default.Size(11));

                        column.Item().PaddingTop(2)
                                  .Text(text =>
                                  {
                                      text.Span("Su diagnostico fue: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                      text.Span(string.Join(", ", consulta.Diagnosticos.Select(d => d.NombreDiagnostico)).ToString()).FontSize(11); // Valor de la fecha sin negrita
                                  });
                        // Espaciador visual
                        column.Item().PaddingVertical(20).BorderBottom(1).LineHorizontal(1);

                        // Espacio para la firma y sello
                        column.Item().PaddingVertical(20).Row(row =>
                        {
                            row.RelativeColumn().Text($"Dr. {consulta.NombresMedico} {consulta.ApellidosMedico}", TextStyle.Default.Size(14).Bold());
                            row.RelativeColumn().Text("Firma y sello").FontSize(14).Bold().AlignRight();
                        });
                    });

                    // Pie de página centrado
                    page.Footer().AlignCenter().Text($"Emitido el {DateTime.Now.ToString("dd/MM/yyyy")}", TextStyle.Default.Italic().Size(12).Color("#808080"));
                });


            });
        }


        private IDocument CreateConsultaDocument(ConsultDetails consulta)
        {
            // Tamaño de página A4 estándar
            return Document.Create(container =>
            {

                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(598, 845);

                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    // Header con una tabla de 6 columnas
                    page.Header().Border(2).BorderColor("#808080").Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100); // Establecimiento
                            columns.ConstantColumn(100); // Nombre
                            columns.ConstantColumn(100); // Apellido
                            columns.ConstantColumn(70); // Sexo
                            columns.ConstantColumn(70); // Edad
                            columns.ConstantColumn(118); // Nº Historia Clínica
                        });

                        // Fila de encabezados
                        table.Cell().Border(1).BorderColor("#808080").Element(CellStyle => CellStyle.Background("#ccffcc"))
                            .MinHeight(14).AlignCenter().PaddingTop(3).Text("ESTABLECIMIENTO").FontSize(6);
                        table.Cell().Border(1).BorderColor("#808080").Element(CellStyle => CellStyle.Background("#ccffcc"))
                            .MinHeight(14).AlignCenter().PaddingTop(3).Text("NOMBRE").FontSize(6);
                        table.Cell().Border(1).BorderColor("#808080").Element(CellStyle => CellStyle.Background("#ccffcc"))
                            .MinHeight(14).AlignCenter().PaddingTop(3).Text("APELLIDO").FontSize(6);
                        table.Cell().Border(1).BorderColor("#808080").Element(CellStyle => CellStyle.Background("#ccffcc"))
                            .MinHeight(14).AlignCenter().PaddingTop(3).Text("SEXO").FontSize(6);
                        table.Cell().Border(1).BorderColor("#808080").Element(CellStyle => CellStyle.Background("#ccffcc"))
                            .MinHeight(14).AlignCenter().PaddingTop(3).Text("EDAD").FontSize(6);
                        table.Cell().Border(1).BorderColor("#808080").Element(CellStyle => CellStyle.Background("#ccffcc"))
                            .MinHeight(14).AlignCenter().PaddingTop(3).Text("Nº HISTORIA CLÍNICA").FontSize(6);






                        // Fila de contenido
                        table.Cell().Border(1).BorderColor("#808080").MinHeight(7).AlignCenter().PaddingTop(3).Element(CellStyle => CellStyle.Background("#FFFFFF")).Text(consulta.DireccionPacientes)
                            .FontSize(7);
                        table.Cell().Border(1).BorderColor("#808080").MinHeight(7).AlignCenter().PaddingTop(3)
                            .Element(CellStyle => CellStyle.Background("#FFFFFF")).Text(consulta.PrimerNombrePaciente).FontSize(7);
                        table.Cell().Border(1).BorderColor("#808080").MinHeight(7).AlignCenter().PaddingTop(3)
                            .Element(CellStyle => CellStyle.Background("#FFFFFF")).Text(consulta.PrimerApellidoPaciente).FontSize(7);
                        table.Cell().Border(1).BorderColor("#808080").MinHeight(7).AlignCenter().PaddingTop(3)
                            .Element(CellStyle => CellStyle.Background("#FFFFFF")).Text(consulta.GeneroPaciente).FontSize(7);
                        table.Cell().Border(1).BorderColor("#808080").MinHeight(7).AlignCenter().PaddingTop(3)
                            .Element(CellStyle => CellStyle.Background("#FFFFFF")).Text(consulta.EdadPacientes).FontSize(7);
                        table.Cell().Border(1).BorderColor("#808080").MinHeight(7).AlignCenter().PaddingTop(3)
                            .Element(CellStyle => CellStyle.Background("#FFFFFF")).Text(consulta.CiPacientes).FontSize(7);
                    });

                    // Contenido principal con múltiples tablas
                    page.Content().PaddingTop(6).Column(contentColumn =>
                    {
                        // Primera tabla
                        contentColumn.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(557);
                            });

                            // Fila de encabezado
                            table.Cell().MinHeight(14).Border(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).PaddingLeft(3).Text("1. MOTIVO DE CONSULTA").FontSize(10).Bold();

                            // Fila de datos
                            table.Cell().MinHeight(14).Border(2).BorderColor(Colors.Grey.Medium).Text($"{consulta.MotivoConsulta}").FontSize(10);
                        });

                        // Segunda tabla
                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(557);
                            });

                            // Fila de encabezado
                            table.Cell().MinHeight(14).Border(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).PaddingLeft(3).Text("2. ANTECEDENTES PERSONALES").FontSize(10).Bold();

                            // Fila de datos
                            table.Cell().MinHeight(14).BorderLeft(2).BorderBottom(1).BorderRight(2).BorderColor("#808080").Text($"{consulta.AntecedentesPersonales}").FontSize(10);
                            // Celda para Alergias
                            // Celda para Alergias
                            table.Cell().MinHeight(14).BorderLeft(2).BorderBottom(1).BorderRight(2).BorderColor("#808080")
                                .Column(column =>
                                {
                                    if (consulta.Alergias != null && consulta.Alergias.Any())
                                    {
                                        // Unir los nombres de las alergias en una sola oración
                                        var alergiasTexto = string.Join(", ", consulta.Alergias.Select(alergia => alergia.NombreAlergia));
                                        column.Item().Text(text =>
                                        {
                                            // Usar Span para "Alergias:" en negrita
                                            text.Span("Alergias:").Bold().FontSize(10);
                                            // Usar Span para el texto de alergias sin negrita
                                            text.Span($" {alergiasTexto}.").FontSize(8); // Asegúrate de que el tamaño de fuente esté correcto
                                        });
                                    }
                                    else
                                    {
                                        column.Item().Text("Alergias: No se registraron alergias.").FontSize(10);
                                    }
                                });




                            // Celda para Cirugías
                            table.Cell().MinHeight(14).BorderLeft(2).BorderBottom(1).BorderRight(2).BorderColor("#808080")
                                .Column(column =>
                                {
                                    if (consulta.Cirugias != null && consulta.Cirugias.Any())
                                    {
                                        // Unir los nombres de las cirugías en una sola oración
                                        var cirugiasTexto = string.Join(", ", consulta.Cirugias.Select(cirugia => cirugia.NombreCirugia));
                                        column.Item().Text(text =>
                                        {
                                            text.Span("Cirugías:").Bold().FontSize(10); // "Cirugías:" en negrita
                                            text.Span($" {cirugiasTexto}.").FontSize(8); // Resto del texto sin negrita
                                        });
                                    }
                                    else
                                    {
                                        column.Item().Text("Cirugías: No se registraron cirugías.").FontSize(10);
                                    }
                                });





                        });

                        // Tercera tabla

                        contentColumn.Item().PaddingTop(7).Border(2).BorderColor("808080").Table(table =>
                        {
                            // Definir las columnas de la tabla para el encabezado
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(557); // Encabezado general ocupa toda la fila
                            });

                            // Fila de encabezado general "3 ANTECEDENTES FAMILIARES"
                            table.Cell().MinHeight(14).BorderLeft(2).BorderRight(2).BorderTop(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).Text("3 ANTECEDENTES FAMILIARES").FontSize(10).Bold();

                            table.Cell().Element(CellStyle =>
                            {
                                // Crear una tabla interna con varias columnas dentro de la celda "padre"
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Añadir columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(27); // Primera columna
                                        columns.ConstantColumn(28); // Segunda columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(28); // Tercera columna
                                        columns.ConstantColumn(29); // Tercera columna
                                        columns.ConstantColumn(24); // Tercera columna


                                    });

                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().BorderRight(1).BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("1.\nCARDIOPATIA").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.Cardiopatia == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("2. \nDIABETES").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.Diabetes == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(15).MinWidth(3).Text("3. ENF.CARDIOVASCULAR\n").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.EnfCardiovascular == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("4.  HIPERTENSION").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.Hipertension == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("5.\nCANCER").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.Cancer == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("6. TUBERCULOSIS").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.Tuberculosis == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("7.ENF MENTAL").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.EnfMental == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("8. ENF INFECCIOSA").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.EnfInfecciosa == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("9. MAL FORMACION").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.MalFormacion == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("10 OTRO").FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).PaddingTop(6).Text(consulta.AntecedentesFamiliares.Otro == true ? "X" : "").FontSize(7).AlignCenter();

                                });
                            });

                            // Crear las observaciones para cada patología con parentesco u observación
                            var observaciones = new List<string>();

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoCardiopatia) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObserCardiopatia))
                            {
                                observaciones.Add("Cardiopatía: " + consulta.AntecedentesFamiliares.ParentescoCardiopatia + " - " + consulta.AntecedentesFamiliares.ObserCardiopatia);
                            }

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoDiabetes) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObserDiabetes))
                            {
                                observaciones.Add("Diabetes: " + consulta.AntecedentesFamiliares.ParentescoDiabetes + " - " + consulta.AntecedentesFamiliares.ObserDiabetes);
                            }

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoEnCardiovascular) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObseEnfCardiovascular))
                            {
                                observaciones.Add("Enf. Cardiovascular: " + consulta.AntecedentesFamiliares.ParentescoEnCardiovascular + " - " + consulta.AntecedentesFamiliares.ObseEnfCardiovascular);
                            }

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoHipertension) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObserHipertension))
                            {
                                observaciones.Add("Hipertensión: " + consulta.AntecedentesFamiliares.ParentescoHipertension + " - " + consulta.AntecedentesFamiliares.ObserHipertension);
                            }
                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoCancer) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObseCancer))
                            {
                                observaciones.Add("Cancer: " + consulta.AntecedentesFamiliares.ParentescoCancer + " - " + consulta.AntecedentesFamiliares.ObseCancer);
                            }

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoTuberculosis) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObserTuberculosis))
                            {
                                observaciones.Add("Tuberculosis: " + consulta.AntecedentesFamiliares.ParentescoTuberculosis + " - " + consulta.AntecedentesFamiliares.ObserTuberculosis);
                            }


                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoEnfMental) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObseEnfMenta))
                            {
                                observaciones.Add("Enf. Mental: " + consulta.AntecedentesFamiliares.ParentescoEnfMental + " - " + consulta.AntecedentesFamiliares.ObseEnfMenta);
                            }

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoEnfInfecciosa) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObsEnfInfecciosa))
                            {
                                observaciones.Add("Enf. Infecciosa: " + consulta.AntecedentesFamiliares.ParentescoEnfInfecciosa + " - " + consulta.AntecedentesFamiliares.ObsEnfInfecciosa);
                            }

                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoMalFormacion) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObserMalFormacion))
                            {
                                observaciones.Add("Mal Formación: " + consulta.AntecedentesFamiliares.ParentescoMalFormacion + " - " + consulta.AntecedentesFamiliares.ObserMalFormacion);
                            }
                            if (!string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ParentescoMalFormacion) || !string.IsNullOrEmpty(consulta.AntecedentesFamiliares.ObserMalFormacion))
                            {
                                observaciones.Add("Otro: " + consulta.AntecedentesFamiliares.ParentescoOtro + " - " + consulta.AntecedentesFamiliares.ObseOtro);
                            }
                            // Agregar más condiciones para las demás patologías si es necesario...

                            // Renderizar observaciones en la tabla si hay alguna
                            if (observaciones.Any())
                            {
                                foreach (var observacion in observaciones)
                                {
                                    // Convertir el texto en tipo oración
                                    var observacionFormateada = char.ToUpper(observacion[0]) + observacion.Substring(1).ToLower();

                                    table.Cell().BorderLeft(2).BorderBottom(1).BorderRight(2).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                        .Text(observacionFormateada).FontSize(9).AlignStart();
                                }

                            }
                            else
                            {
                                table.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                    .Text("").FontSize(9).AlignStart();
                            }



                            table.Cell().MinHeight(16).BorderLeft(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Text("").FontSize(10);


                        });
                        //CUARTA TABLA
                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(557);
                            });

                            // Fila de encabezado
                            table.Cell().MinHeight(14).Border(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).PaddingLeft(3).Text("4 ENFERMEDAD O PROBLEMA ACTUAL").FontSize(10).Bold();

                            var texto = consulta.EnfermedadConsulta;

                            // Definir un límite de caracteres que se ajuste a una celda.
                            var limiteCaracteresPorFila = 700;

                            // Dividir el texto en fragmentos según el límite
                            var partesTexto = Enumerable.Range(0, (texto.Length + limiteCaracteresPorFila - 1) / limiteCaracteresPorFila)
                                                        .Select(i => texto.Substring(i * limiteCaracteresPorFila, Math.Min(limiteCaracteresPorFila, texto.Length - i * limiteCaracteresPorFila)))
                                                        .ToList();

                            // Generar las filas dinámicamente
                            foreach (var parte in partesTexto)
                            {
                                table.Cell().BorderLeft(2).MinHeight(14).BorderBottom(1).BorderRight(2).BorderColor("#808080").Text(parte).FontSize(10);

                                // Las siguientes celdas serán "quemadas" (vacías)
                                table.Cell().BorderLeft(2).MinHeight(14).BorderBottom(1).BorderRight(2).BorderColor("#808080").Text("").FontSize(10);
                                table.Cell().BorderLeft(2).MinHeight(14).BorderBottom(1).BorderRight(2).BorderColor("#808080").Text("").FontSize(10);
                                table.Cell().BorderLeft(2).MinHeight(14).BorderBottom(1).BorderRight(2).BorderColor("#808080").Text("").FontSize(10);
                                table.Cell().BorderLeft(2).MinHeight(14).BorderBottom(1).BorderRight(2).BorderColor("#808080").Text("").FontSize(10);
                                table.Cell().BorderLeft(2).MinHeight(14).BorderBottom(2).BorderRight(2).BorderColor("#808080").Text("").FontSize(10);
                            }
                        });
                        //QUINTA TABLA
                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            // Definir las columnas de la tabla para el encabezado
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(557); // Encabezado general ocupa toda la fila
                            });

                            // Fila de encabezado general "3 ANTECEDENTES FAMILIARES"
                            table.Cell().MinHeight(14).BorderLeft(2).BorderRight(2).BorderTop(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(3).Text("5 REVISIÓN ACTUAL DE ÓRGANOS Y SISTEMAS").FontSize(10).Bold();

                            table.Cell().Element(CellStyle =>
                            {
                                // Crear una tabla interna con varias columnas dentro de la celda "padre"
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Añadir columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {

                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(17); // Tercera columna


                                    });

                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").FontSize(5).Bold().AlignEnd();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).Bold().AlignEnd();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).Bold().AlignEnd();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).Bold().AlignEnd();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).Bold().AlignEnd();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();



                                });
                            });
                            table.Cell().Element(CellStyle =>
                            {
                                // Crear una tabla interna con varias columnas dentro de la celda "padre"
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Añadir columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {

                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna
                                        columns.ConstantColumn(79); // Tercera columna
                                        columns.ConstantColumn(17); // Tercera columna
                                        columns.ConstantColumn(16); // Tercera columna


                                    });

                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().BorderRight(1).BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("1 ÓRGANO DE LOS\r\nSENTIDOS").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.OrgSentidos == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.OrgSentidos == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("3 CARDIO\r\nVASCULAR").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.CardioVascular == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.CardioVascular == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("5.  GENITAL").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Genital == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Genital == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("7. MÚSCULO\r\nESQUELÉTICO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.MEsqueletico == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.MEsqueletico == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("9. HEMO LINFÁTICO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Linfatico == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Linfatico == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("2. RESPIRATORIO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Respiratorio == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Respiratorio == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("4. DIGESTIVO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Digestivo == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Digestivo == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("6. URINARIO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Urinario == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Urinario == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("8. ENDOCRINO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Endocrino == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Endocrino == true ? "" : "X").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("10. NERVIOSO").FontSize(6).Bold().AlignEnd();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Nervioso == true ? "X" : "").FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(10).MinWidth(3).Text(consulta.OrganosSistemas.Nervioso == true ? "" : "X").FontSize(7).AlignCenter();
                                });
                            });
                            // Lista de observaciones (puedes ajustarla según tu modelo de datos real)
                            var observaciones = new List<string>
{
    consulta.OrganosSistemas.ObserOrgSentidos,
    consulta.OrganosSistemas.ObserCardiovascular,
    consulta.OrganosSistemas.ObserGenital,
    consulta.OrganosSistemas.ObserMEsqueletico,
    consulta.OrganosSistemas.ObserLinfatico,
    consulta.OrganosSistemas.ObserRespiratorio,
    consulta.OrganosSistemas.ObserDigestivo,
    consulta.OrganosSistemas.ObserUrinario,
    consulta.OrganosSistemas.ObserEndocrino,
    consulta.OrganosSistemas.ObserNervioso,
    // Agrega más observaciones aquí
};

                            // Iterar sobre las observaciones para generar las filas dinámicamente
                            foreach (var observacion in observaciones.Where(o => !string.IsNullOrEmpty(o)))
                            {
                                // Primera celda con la observación
                                table.Cell()
                                    .MinHeight(13)
                                    .BorderLeft(2)
                                    .BorderRight(2)
                                    .BorderBottom(1)
                                    .BorderColor("#808080")
                                    .Text(observacion)
                                    .FontSize(10);

                                // Las siguientes celdas están quemadas (vacías)
                            }



                        });
                        //SEXTA TABLA
                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            // Definir las columnas de la tabla para el encabezado
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(557); // Encabezado general ocupa toda la fila
                            });

                            // Fila de encabezado general "3 ANTECEDENTES FAMILIARES"
                            table.Cell().MinHeight(14).BorderLeft(2).BorderRight(2).BorderTop(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(3).Text("6 SIGNOS VITALES Y ANTROPOMETRIA").FontSize(10).Bold();
                            table.Cell().Element(CellStyle =>
                            {
                                // Crear una tabla interna con varias columnas dentro de la celda "padre"
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Añadir columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {

                                        columns.ConstantColumn(92); // Tercera columna
                                        columns.ConstantColumn(92); // Tercera columna
                                        columns.ConstantColumn(92); // Tercera columna
                                        columns.ConstantColumn(92); // Tercera columna
                                        columns.ConstantColumn(92); // Tercera columna
                                        columns.ConstantColumn(95); // Tercera columna


                                    });

                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("FECHA DE MEDICIÓN").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).AlignCenter().Text(consulta.FechaCreacion).Bold().FontSize(7);
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("TEMPERATURA °C").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.TemperaturaConsulta).Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();


                                });
                            });
                            table.Cell().Element(CellStyle =>
                            {
                                // Crear una tabla interna con varias columnas dentro de la celda "padre"
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Añadir columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {

                                        columns.ConstantColumn(92); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(47); // Tercera columna


                                    });

                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().BorderRight(1).BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("PRESIÓN ARTERIAL").FontSize(5).Bold().AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.PresionSistolica).FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.PresionDiastolica).FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();

                                });
                            });

                            table.Cell().BorderBottom(2).BorderColor("808080").Element(CellStyle =>
                            {
                                // Crear una tabla interna con varias columnas dentro de la celda "padre"
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Añadir columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {

                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(46); // Tercera columna
                                        columns.ConstantColumn(47); // Tercera columna


                                    });

                                    // Fila dentro de la tabla anidada 
                                    nestedTable.Cell().BorderRight(1).BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("PULSO / min").FontSize(5).Bold().AlignCenter();
                                    nestedTable.Cell().BorderRight(1).BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("FRECUENCIA\r\nRESPIRATORIA").FontSize(5).Bold().AlignCenter();

                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.PulsoConsulta).FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.FrecuenciaRespiratoria).FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();

                                    nestedTable.Cell().BorderRight(1).BorderBottom(2).BorderColor("#808080").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("PESO / Kg").FontSize(5).Bold().AlignCenter();
                                    nestedTable.Cell().BorderRight(1).BorderBottom(2).BorderColor("#808080").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("TALLA / cm").FontSize(5).Bold().AlignCenter();

                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.PesoConsulta).FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text(consulta.TallaConsulta).FontSize(7).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(10).MinWidth(3).Text("").FontSize(4).AlignCenter();

                                });
                            });



                        });

                        //SEPTIMA TABLA
                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            // Definir las columnas de la tabla principal para el encabezado
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(185); // Primera columna
                                columns.ConstantColumn(185); // Segunda columna
                                columns.ConstantColumn(187); // Tercera columna
                            });

                            // Fila de encabezado "7 EXAMEN FÍSICO REGIONAL"
                            table.Cell().MinHeight(14).BorderLeft(2).BorderTop(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(3).Text("7 EXAMEN FÍSICO REGIONAL ").FontSize(10).Bold();

                            table.Cell().MinHeight(14).BorderTop(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(70).Text("CP = CON EVIDENCIA DE PATOLOGÍA: MARCAR \"X\" Y DESCRIBIR ").FontSize(6).Bold().AlignCenter();

                            table.Cell().MinHeight(14).BorderRight(2).BorderTop(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(65).Text("SP = SIN EVIDENCIA DE PATOLOGÍA:\r\n MARCAR \"X\" Y NO DESCRIBIR\r\n").FontSize(6).Bold().AlignCenter();

                            // Aquí creamos una nueva celda para la tabla interna con 18 columnas
                            table.Cell().ColumnSpan(3).Element(CellStyle =>
                            {
                                // Crear una tabla interna con 18 columnas
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Definir 18 columnas dentro de la tabla anidada
                                    nestedTable.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(70);  // Columna 1
                                        columns.ConstantColumn(16);  // Columna 2
                                        columns.ConstantColumn(10);  // Columna 3
                                        columns.ConstantColumn(70);  // Columna 4
                                        columns.ConstantColumn(10);  // Columna 5
                                        columns.ConstantColumn(10);  // Columna 6
                                        columns.ConstantColumn(70);  // Columna 7
                                        columns.ConstantColumn(10);  // Columna 8
                                        columns.ConstantColumn(10);  // Columna 9
                                        columns.ConstantColumn(70);  // Columna 10
                                        columns.ConstantColumn(10);  // Columna 11
                                        columns.ConstantColumn(10);  // Columna 12
                                        columns.ConstantColumn(70);  // Columna 13
                                        columns.ConstantColumn(10);  // Columna 14
                                        columns.ConstantColumn(10);  // Columna 15
                                        columns.ConstantColumn(79);  // Columna 16
                                        columns.ConstantColumn(10);  // Columna 17
                                        columns.ConstantColumn(10);  // Columna 18
                                    });

                                    // Fila dentro de la tabla anidada con 18 celdas creadas manualmente
                                    // Aquí agregas todas las celdas para la tabla de 18 columnas
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderLeft(2).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderLeft(2).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderLeft(2).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderLeft(2).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderLeft(2).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("CP").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderLeft(2).BorderBottom(1).BorderColor("#C6C2C2").Background("#99ccff").MinHeight(10).MinWidth(3).Text("SP").Bold().FontSize(5).AlignCenter();
                                    // Continúa agregando las celdas necesarias
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3).Text("1. CABEZA").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Cabeza == true ? "X" : "").Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Cabeza == true ? "" : "X").Bold().FontSize(7).AlignCenter();

                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3).Text("2. CUELLO").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Cuello == true ? "X" : "").Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Cuello == true ? "" : "X").Bold().FontSize(7).AlignCenter();

                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(10).MinWidth(3).Text("3. TÓRAX").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Torax == true ? "X" : "").Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Torax == true ? "" : "X").Bold().FontSize(7).AlignCenter();

                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3).Text("4. ABDOMEN").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Abdomen == true ? "X" : "").Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Abdomen == true ? "" : "X").Bold().FontSize(7).AlignCenter();

                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3).Text("5. PELVIS").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Pelvis == true ? "X" : "").Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Pelvis == true ? "" : "X").Bold().FontSize(7).AlignCenter();

                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3).Text("6 . EXTREMIDADES").Bold().FontSize(5).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Extremidades == true ? "X" : "").Bold().FontSize(7).AlignCenter();
                                    nestedTable.Cell().BorderTop(1).BorderBottom(1).BorderLeft(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12).MinWidth(3).Text(consulta.ExamenFisico.Extremidades == true ? "" : "X").Bold().FontSize(7).AlignCenter();


                                });
                            });

                            // Aquí agregamos una segunda tabla anidada que abarque todo el ancho
                            table.Cell().ColumnSpan(3).Element(CellStyle =>
                            {
                                // Crear una tabla interna con una sola columna
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderBottom(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Definir una sola columna
                                    nestedTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1); // Una columna que abarca todo el ancho
                                    });

                                    if (!string.IsNullOrEmpty(consulta.ExamenFisico.ObserCabeza))
                                    {
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                            .Text(consulta.ExamenFisico.ObserCabeza).FontSize(9).AlignStart();
                                    }

                                    if (!string.IsNullOrEmpty(consulta.ExamenFisico.ObserCuello))
                                    {
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                            .Text(consulta.ExamenFisico.ObserCuello).FontSize(9).AlignStart();
                                    }

                                    if (!string.IsNullOrEmpty(consulta.ExamenFisico.ObserTorax))
                                    {
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                            .Text(consulta.ExamenFisico.ObserTorax).FontSize(9).AlignStart();
                                    }

                                    if (!string.IsNullOrEmpty(consulta.ExamenFisico.ObserAbdomen))
                                    {
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                            .Text(consulta.ExamenFisico.ObserAbdomen).FontSize(9).AlignStart();
                                    }

                                    if (!string.IsNullOrEmpty(consulta.ExamenFisico.ObserPelvis))
                                    {
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                            .Text(consulta.ExamenFisico.ObserPelvis).FontSize(9).AlignStart();
                                    }

                                    if (!string.IsNullOrEmpty(consulta.ExamenFisico.ObserExtremidades))
                                    {
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#FFFFFF").MinHeight(12).MinWidth(3)
                                            .Text(consulta.ExamenFisico.ObserExtremidades).FontSize(9).AlignStart();
                                    }


                                });
                            });
                        });

                        //OCTAVA TABLA

                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            // Definir las columnas de la tabla principal con tamaños específicos
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(170);  // Columna 1 (Diagnóstico 1)
                                columns.ConstantColumn(95);   // Columna 2 (CIE Diagnóstico 1)
                                columns.ConstantColumn(19);   // Columna 3 (PRE Diagnóstico 1)
                                columns.ConstantColumn(20);   // Columna 4 (DEF Diagnóstico 1)
                                columns.ConstantColumn(12);   // Espacio entre diagnósticos
                                columns.ConstantColumn(193);  // Columna 5 (Diagnóstico 2)
                                columns.ConstantColumn(13);   // Columna 6 (CIE Diagnóstico 2)
                                columns.ConstantColumn(16);   // Columna 7 (PRE Diagnóstico 2)
                                columns.ConstantColumn(18);   // Columna 8 (DEF Diagnóstico 2)
                            });

                            // Fila de encabezado "8 DIAGNOSTICO"
                            table.Cell().MinHeight(14).BorderLeft(2).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(3).Text("8 DIAGNOSTICO").FontSize(10).Bold();

                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).Text("PRE = PRESUNTIVO\r\nDEF = DEFINITIVO").FontSize(7).Bold();

                            // Encabezados de la tabla
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("CIE").FontSize(6).Bold();
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("PRE").FontSize(6).Bold();
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("DEF").FontSize(6).Bold();
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("").FontSize(6).Bold();
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("CIE").FontSize(6).Bold();
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("PRE").FontSize(6).Bold();
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignCenter().PaddingTop(3).MinWidth(2).Text("DEF").FontSize(6).Bold();

                            // Crear una tabla dentro de la celda para los diagnósticos
                            table.Cell().ColumnSpan(9).Element(CellStyle =>
                            {
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderBottom(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Definir columnas dentro de la subtabla para los diagnósticos
                                    nestedTable.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(14);  // Columna 1
                                        columns.ConstantColumn(250); // Columna 2
                                        columns.ConstantColumn(20);  // Columna 3
                                        columns.ConstantColumn(18);  // Columna 4
                                        columns.ConstantColumn(16);  // Columna 5
                                        columns.ConstantColumn(14);  // Columna 6
                                        columns.RelativeColumn(2);   // Columna 7
                                        columns.ConstantColumn(18);  // Columna 8
                                        columns.ConstantColumn(18);  // Columna 9
                                        columns.ConstantColumn(20);  // Columna 10
                                    });

                                    // Agregar diagnósticos en pares
                                    int rowIndex = 1;
                                    for (int i = 0; i < consulta.Diagnosticos.Count; i += 2)
                                    {
                                        var diagnostico1 = consulta.Diagnosticos[i]; // Primer diagnóstico en la fila
                                        var diagnostico2 = (i + 1 < consulta.Diagnosticos.Count) ? consulta.Diagnosticos[i + 1] : null; 
                                        // Segundo diagnóstico (si existe)
                                                                                                                                        // Agregar celdas para cada columna en la misma fila
                                                                                                                                        // Columna 1 (Número de fila o algún identificador)
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3)
                                            .Text(rowIndex.ToString()).FontSize(9).AlignCenter();

                                        // Columna 1: Diagnóstico 1
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(12)
                                            .Text(diagnostico1.NombreDiagnostico).FontSize(9).AlignCenter();

                                        // Columna 2: CIE10 Diagnóstico 1
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(12)
                                            .Text(diagnostico1.CIE10DIA.ToString()).FontSize(9).AlignCenter();

                                        // Columna 3: Presuntivo Diagnóstico 1
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12)
                                            .Text(diagnostico1.PresuntivoDiagnosticos ? "X" : "").FontSize(9).AlignCenter();

                                        // Columna 4: Definitivo Diagnóstico 1
                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12)
                                            .Text(diagnostico1.DefinitivoDiagnosticos ? "X" : "").FontSize(9).AlignCenter();

                                        nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ccffcc").MinHeight(12).MinWidth(3)
                                           .Text(rowIndex.ToString()).FontSize(9).AlignCenter();

                                        if (diagnostico2 != null)
                                        {
                                            
                                            // Columna 5: Diagnóstico 2 (si existe)
                                            nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(12)
                                                .Text(diagnostico2.NombreDiagnostico).FontSize(9).AlignCenter();

                                            // Columna 6: CIE10 Diagnóstico 2
                                            nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(12)
                                                .Text(diagnostico2.CIE10DIA.ToString()).FontSize(9).AlignCenter();

                                            // Columna 7: Presuntivo Diagnóstico 2
                                            nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12)
                                                .Text(diagnostico2.PresuntivoDiagnosticos ? "X" : "").FontSize(9).AlignCenter();

                                            // Columna 8: Definitivo Diagnóstico 2
                                            nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffff99").MinHeight(12)
                                                .Text(diagnostico2.DefinitivoDiagnosticos ? "X" : "").FontSize(9).AlignCenter();
                                        }
                                        else
                                        {
                                            // Si no hay un segundo diagnóstico, llenar las celdas con espacios vacíos
                                            for (int j = 0; j < 4; j++)
                                            {
                                                nestedTable.Cell().Border(1).BorderColor("#ffffff").Background("#ffffff").MinHeight(12).Text("");
                                            }
                                        }
                                        rowIndex++;
                                    }
                                 
                                });
                            });
                        });


                        //Novena TABLA

                        contentColumn.Item().PaddingTop(7).Table(table =>
                        {
                            // Definir las columnas de la tabla principal con dos columnas
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(278);  // Columna 1
                                columns.ConstantColumn(278);  // Columna 2
                            });

                            // Fila de encabezado "9 PLANES DE TRATAMIENTO"
                            table.Cell().MinHeight(14).BorderLeft(2).BorderTop(2).BorderBottom(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignLeft().PaddingTop(3).PaddingLeft(3).Text("9 PLANES DE TRATAMIENTO ").FontSize(10).Bold();

                            // Segunda columna con la descripción
                            table.Cell().MinHeight(14).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Element(CellStyle =>
                                CellStyle.Background("#ccccff")).AlignRight().PaddingTop(3).Text("REGISTRAR LOS PLANES: DIAGNOSTICO, TERAPÉUTICO Y\r\nEDUCACIONAL").FontSize(7);

                            // Subtabla debajo del encabezado que abarca todo el ancho
                            table.Cell().ColumnSpan(2).Element(CellStyle =>
                            {
                                // Crear una subtabla con una columna y cuatro filas de manera estática
                                CellStyle.Background("#ffffff").BorderLeft(2).BorderRight(2).BorderBottom(2).BorderColor("#808080").Table(nestedTable =>
                                {
                                    // Definir una sola columna en la subtabla
                                    nestedTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);  // Una columna que abarca todo el ancho
                                    });

                                    // Primera fila
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(20).MinWidth(3).PaddingTop(3)
                                        .Text(consulta.PlanTratamiento).FontSize(9).AlignLeft();

                                    // Segunda fila
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(20).MinWidth(3)
                                        .Text(" ").FontSize(9).AlignLeft();

                                    // Tercera fila
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(20).MinWidth(3)
                                        .Text("").FontSize(9).AlignLeft();

                                    // Cuarta fila
                                    nestedTable.Cell().Border(1).BorderColor("#C6C2C2").Background("#ffffff").MinHeight(20).MinWidth(3)
                                        .Text("").FontSize(9).AlignLeft();
                                });
                            });
                        });

                        contentColumn.Item().PaddingTop(50).Table(table =>
                        {
                            // Definir las columnas de la tabla principal con medidas específicas en puntos (pt)
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(54);  // Columna 1 (FECHA)
                                columns.ConstantColumn(57);  // Columna 2 (Valor de FECHA)
                                columns.ConstantColumn(30);  // Columna 3 (HORA)
                                columns.ConstantColumn(54);  // Columna 4 (Valor de HORA)
                                columns.ConstantColumn(57);  // Columna 5 (NOMBRE DEL PROFESIONAL)
                                columns.ConstantColumn(100); // Columna 6 (Valor del NOMBRE)
                                columns.ConstantColumn(57);  // Columna 7 (Número del Profesional)
                                columns.ConstantColumn(50);  // Columna 8 (FIRMA)
                                columns.ConstantColumn(40);  // Columna 9 (Campo vacío para FIRMA)
                                columns.ConstantColumn(30);  // Columna 10 (HOJA)
                                columns.ConstantColumn(22);  // Columna 11 (Valor de HOJA)
                            });

                            // Fila con las celdas del content que abarcan el ancho completo de la página
                            table.Cell().Element(CellStyle => CellStyle.Background("#ccffcc").Border(1)).AlignCenter().Text("FECHA").FontSize(8);
                            table.Cell().Element(CellStyle => CellStyle.Background("#FFFFFF").Border(1)).AlignCenter().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(8);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ccffcc").Border(1)).AlignCenter().Text("HORA").FontSize(8);
                            table.Cell()
          .Background("#ffffff")
          .Border(1)
          .AlignCenter()
          .Text(DateTime.Now.ToString("HH:mm"))  // Formato de hora en 24 horas
          .FontSize(8);

                            table.Cell().Element(CellStyle => CellStyle.Background("#ccffcc").Border(1)).AlignCenter().Text("NOMBRE DEL\r\nPROFESIONAL").FontSize(7);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ffffff").Border(1)).AlignCenter().Text(consulta.NombresMedico + consulta.ApellidosMedico).FontSize(6);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ffffff").Border(1)).AlignCenter().Text(consulta.CodigoMedico).FontSize(8);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ccffcc").Border(1)).AlignCenter().Text("FIRMA").FontSize(8);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ffffff").Border(1)).AlignCenter().Text("").FontSize(8);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ccffcc").Border(1)).AlignCenter().Text("NUMERO DE HOJA").FontSize(4);
                            table.Cell().Element(CellStyle => CellStyle.Background("#ffffff").Border(1)).AlignCenter().Text("1").FontSize(8);
                        });



                    });

                    // Footer de la página
                    // Footer de la página
                    page.Footer().Height(20).PaddingHorizontal(2).Row(row =>
                    {
                        // Texto a la izquierda
                        row.RelativeItem().AlignLeft().Text(text =>
                        {
                            text.Span("SNS-MSP / HCU-form.002 / 2008")
                                .FontSize(7)
                                .Bold();
                        });

                        // Texto a la derecha
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("CONSULTA EXTERNA - ANAMNESIS Y EXAMEN FÍSICO")
                                .FontSize(9)
                                .Bold();
                        });
                    });






                });
                // Segunda página

                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(598, 845);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    // Contenido de la segunda página con una sola tabla de 5 columnas
                    page.Content().Column(column =>
                    {
                        // Primera tabla de cinco columnas
                        column.Item().Table(table =>
                        {
                            // Definir las columnas de la tabla principal con cinco columnas
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(136);  // Columna 1
                                columns.ConstantColumn(136);  // Columna 2
                                columns.ConstantColumn(10);   // Columna 3 (espaciador)
                                columns.ConstantColumn(136);  // Columna 4
                                columns.ConstantColumn(136);  // Columna 5
                            });

                            // Fila de datos
                            table.Cell().MinHeight(15).BorderLeft(2).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#ccccff")
                                .Text("10 EVOLUCIÓN").FontSize(10).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#ccccff")
                                .Text("FIRMAR AL PIE DE CADA NOTA").FontSize(7).AlignEnd();

                            table.Cell().MinHeight(20).BorderColor("#808080").Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderColor("#808080").Background("#ccccff")
                                .Text("11 PRESCRIPCIONES").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#ccccff")
                                .Text("FIRMAR AL PIE DE CADA PRESCRIPCIÓN").FontSize(5).AlignRight();
                        });

                        // Espacio entre tablas
                        column.Item().Text("REGISTRAR EN ROJO LA ADMINISTRACIÓN DE FÁRMACOS Y OTROS PRODUCTOS (ENFERMERÍA)").FontSize(8).Light().AlignEnd();

                        // Primera tabla de cinco columnas
                        column.Item().Table(table =>
                        {
                            // Definir las columnas de la tabla principal con cinco columnas
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);  // Columna 1
                                columns.ConstantColumn(30);  // Columna 2
                                columns.ConstantColumn(182);  // Columna 2
                                columns.ConstantColumn(13);   // Columna 3 (espaciador)
                                columns.ConstantColumn(220);  // Columna 1
                                columns.ConstantColumn(50);  // Columna 2

                            });

                            // Fila de datos
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#ccffcc")
                                .Text("\nFECHA\r\n(DIA/MES/AÑO)").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#ccffcc")
                                .Text("\nHORA").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#ccffcc")
                                .Text("\nNOTAS DE EVOLUCIÓN").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#ccffcc")
                                .Text("FARMACOTERAPIA E INDICACIONES\r\n(PARA ENFERMERÍA Y OTRO PERSONAL)").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#ccffcc")
                                .Text("ADMINISTR.\r\nFÁRMACOS\r\nY OTROS").FontSize(7).AlignCenter();

                            //Notas abajo
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                             .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                       .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderLeft(2).BorderRight(1).BorderTop(2).BorderBottom(2).BorderColor("#808080").Background("#FFFFFF")
                          .Text("").FontSize(8).AlignCenter();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderRight(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter().Bold();

                            table.Cell().MinHeight(15).BorderColor("#808080").BorderLeft(2).BorderRight(2).Background("#ffffff")
                                .Text("").FontSize(9).AlignLeft().Bold();

                            table.Cell().MinHeight(15).BorderTop(2).BorderBottom(2).BorderLeft(2).BorderRight(1).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();

                            table.Cell().MinHeight(20).BorderRight(2).BorderBottom(2).BorderTop(2).BorderColor("#808080").Background("#FFFFFF")
                                .Text("").FontSize(7).AlignCenter();
                        });

                    });




                    // Footer de la segunda página
                    page.Footer().Height(20).PaddingHorizontal(2).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text(text =>
                        {
                            text.Span("SNS-MSP / HCU-form.002 / 2008").FontSize(7).Bold();
                        });

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("CONSULTA EXTERNA - ANAMNESIS Y EXAMEN FÍSICO").FontSize(9).Bold();
                        });
                    });
                });


            });
        }

        private IDocument CreateLaboratorioDocument(ConsultDetails consulta)
        {
            // Formato especializado de laboratorio, tamaño A4 con márgenes pequeños
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Size(1194, 847); // Tamaño A3 horizontal

                    page.DefaultTextStyle(x => x.FontFamily("Arial")); // Cambiar la familia de fuentes a Arial

                    page.Header().Row(row =>
                    {
                        row.ConstantItem(550).Column(col =>
                        {
                            col.Item().Text("Dr:" + consulta.NombresMedico + consulta.ApellidosMedico).Bold().FontSize(14); // Encabezado más grande
                            col.Item().Text("Especialista en: " + consulta.NombreEspecialidad).FontSize(12);
                            col.Item().Text("e-mail: " + consulta.EmailMedico).FontSize(11);
                            col.Item().Text("Teléfonos: " + consulta.TelefonoMedico).FontSize(11);
                            col.Item().PaddingTop(2).Text("").FontSize(11);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("Dr:" + consulta.NombresMedico + consulta.ApellidosMedico).Bold().FontSize(14); // Encabezado más grande
                            col.Item().Text("Especialista en: " + consulta.NombreEspecialidad).FontSize(12);
                            col.Item().Text("e-mail: " + consulta.EmailMedico).FontSize(11);
                            col.Item().Text("Teléfonos: " + consulta.TelefonoMedico).FontSize(11);
                            col.Item().PaddingTop(2).Text("").FontSize(11);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });
                    });


                    page.Content().Column(content =>
                    {
                        // Primera fila
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(400).Column(col =>
                            {
                                col.Item().PaddingTop(10)
                                   .Text(text =>
                                   {
                                       text.Span("Fecha: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                       text.Span(consulta.FechaCreacion.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                   });


                                col.Item().PaddingTop(2)
                                     .Text(text =>
                                     {
                                         text.Span("Apellidos: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                         text.Span(consulta.PrimerApellidoPaciente + " " + consulta.SegundoApellidoPaciente).FontSize(11); // Valor de la fecha sin negrita
                                     });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Nombres: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.PrimerNombrePaciente + " " + consulta.SegundoNombrePaciente).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Edad: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.EdadPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                // Información de diagnósticos
                                col.Item().Text(text =>
                                {
                                    text.Span("Diagnóstico: ").FontSize(11).Bold();

                                    var diagnosticos = consulta.Diagnosticos.Select(d => d.NombreDiagnostico.ToLower()).ToList(); // Convertir a minúsculas
                                    string diagnosticosEnOracion;

                                    if (diagnosticos.Count == 1)
                                    {
                                        diagnosticosEnOracion = diagnosticos[0]; // Solo un diagnóstico
                                    }
                                    else if (diagnosticos.Count == 2)
                                    {
                                        diagnosticosEnOracion = string.Join(" , ", diagnosticos); // Dos diagnósticos, unir con "y"
                                    }
                                    else
                                    {
                                        diagnosticosEnOracion = string.Join(", ", diagnosticos.Take(diagnosticos.Count - 1)) +
                                                                " , " + diagnosticos.Last(); // Varios diagnósticos, "y" antes del último
                                    }

                                    diagnosticosEnOracion = char.ToUpper(diagnosticosEnOracion[0]) + diagnosticosEnOracion.Substring(1); // Capitalizar la primera letra

                                    text.Span(diagnosticosEnOracion).FontSize(11); // Mostrar la oración con los diagnósticos
                                });
                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                // Muestra todos los secuenciales de los medicamentos
                                if (consulta.Medicamentos.Any())
                                {
                                    col.Item().PaddingTop(2)
                                   .Text(text =>
                                   {
                                       text.Span("Receta: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                       text.Span(consulta.Medicamentos.First().SecuencialMedicamento.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                   });
                                }
                                else
                                {
                                    col.Item().PaddingTop(10).Text("Secuencial: No hay medicamentos").Bold().FontSize(14);
                                }

                                col.Item().PaddingTop(2)
                               .Text(text =>
                               {
                                   text.Span("CC: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                   text.Span(consulta.CiPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                               });
                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(400).Column(col =>
                            {
                                col.Item().PaddingTop(10)
                                   .Text(text =>
                                   {
                                       text.Span("Fecha: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                       text.Span(consulta.FechaCreacion.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                   });


                                col.Item().PaddingTop(2)
                                     .Text(text =>
                                     {
                                         text.Span("Apellidos: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                         text.Span(consulta.PrimerApellidoPaciente + " " + consulta.SegundoApellidoPaciente).FontSize(11); // Valor de la fecha sin negrita
                                     });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Nombres: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.PrimerNombrePaciente + " " + consulta.SegundoNombrePaciente).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                col.Item().PaddingTop(2)
                                    .Text(text =>
                                    {
                                        text.Span("Edad: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                        text.Span(consulta.EdadPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                    });
                                // Información de diagnósticos
                                col.Item().Text(text =>
                                {
                                    text.Span("Diagnóstico: ").FontSize(11).Bold();

                                    var diagnosticos = consulta.Diagnosticos.Select(d => d.NombreDiagnostico.ToLower()).ToList(); // Convertir a minúsculas
                                    string diagnosticosEnOracion;

                                    if (diagnosticos.Count == 1)
                                    {
                                        diagnosticosEnOracion = diagnosticos[0]; // Solo un diagnóstico
                                    }
                                    else if (diagnosticos.Count == 2)
                                    {
                                        diagnosticosEnOracion = string.Join(" , ", diagnosticos); // Dos diagnósticos, unir con "y"
                                    }
                                    else
                                    {
                                        diagnosticosEnOracion = string.Join(", ", diagnosticos.Take(diagnosticos.Count - 1)) +
                                                                " , " + diagnosticos.Last(); // Varios diagnósticos, "y" antes del último
                                    }

                                    diagnosticosEnOracion = char.ToUpper(diagnosticosEnOracion[0]) + diagnosticosEnOracion.Substring(1); // Capitalizar la primera letra

                                    text.Span(diagnosticosEnOracion).FontSize(11); // Mostrar la oración con los diagnósticos
                                });
                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                // Muestra todos los secuenciales de los medicamentos
                                if (consulta.Medicamentos.Any())
                                {
                                    col.Item().PaddingTop(2)
                                   .Text(text =>
                                   {
                                       text.Span("Receta: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                       text.Span(consulta.Medicamentos.First().SecuencialMedicamento.ToString()).FontSize(11); // Valor de la fecha sin negrita
                                   });
                                }
                                else
                                {
                                    col.Item().PaddingTop(10).Text("Secuencial: No hay medicamentos").Bold().FontSize(14);
                                }

                                col.Item().PaddingTop(2)
                               .Text(text =>
                               {
                                   text.Span("CC: ").FontSize(11).Bold(); // Solo "Fecha:" en negrita
                                   text.Span(consulta.CiPacientes.ToString()).FontSize(11); // Valor de la fecha sin negrita
                               });
                            });
                        });

                        // Segunda fila (Nueva fila)
                        // Segunda fila (Nueva fila)
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(550).PaddingTop(50).Column(col =>
                            {
                                col.Item().PaddingBottom(6).Text("LABORATORIO").Bold().FontSize(14); // Título
                                                                                    // Recorre la lista de laboratorios y muestra su información
                                foreach (var laboratorio in consulta.Laboratorios)
                                {
                                    // Crear una fila para cada laboratorio
                                    col.Item().Row(innerRow =>
                                    {
                                        innerRow.ConstantItem(400).Column(innerCol =>
                                        {
                                            innerCol.Item().Text(laboratorio.NombreLaboratorio).FontSize(12);
                                        });

                                        innerRow.ConstantItem(40).Column(innerCol =>
                                        {
                                            innerCol.Item().Text("X" + laboratorio.CantidadLaboratorio).FontSize(12); // Cantidad de cada laboratorio
                                        });
                                    });
                                }
                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(550).PaddingTop(50).Column(col =>
                            {
                                col.Item().PaddingBottom(20).Text("OBSERVACIONES").Bold().FontSize(14); // Título
                                                                                                        // Recorre la lista de laboratorios y muestra las observaciones
                                foreach (var laboratorio in consulta.Laboratorios)
                                {
                                    col.Item().MinWidth(300).Text(laboratorio.ObserLaboratorio).FontSize(12);
                                }

                                // Si hay espacio adicional, puedes agregar más celdas vacías si es necesario
                                for (int i = consulta.Laboratorios.Count; i < 12; i++) // Por ejemplo, llenar hasta 12 celdas
                                {
                                    col.Item().MinWidth(300).Text("").FontSize(12);
                                }
                            });
                        });









                    });

                    page.Footer().Column(footer =>
                    {// Nueva fila del footer


                        // Primera fila del footer
                        footer.Item().Row(row =>
                        {
                            row.ConstantItem(530).Column(col =>
                            {
                                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().Text(consulta.DireccionMedico).FontSize(11).AlignCenter();
                            });

                            row.AutoItem().PaddingHorizontal(30).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().Text(consulta.DireccionMedico).FontSize(11).AlignCenter();
                            });
                        });


                    });

                });
            });
        }

        private IDocument CreateImagenDocument(ConsultDetails consulta)
        {
            // Informe de imagenología, tamaño A3 con orientación horizontal
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Size(1194, 847); // Tamaño A3 horizontal

                    page.DefaultTextStyle(x => x.FontFamily("Arial")); // Cambiar la familia de fuentes a Arial

                    page.Header().Row(row =>
                    {
                        row.ConstantItem(550).Column(col =>
                        {
                            col.Item().Text("Dr:" + consulta.NombresMedico + consulta.ApellidosMedico).Bold().FontSize(14); // Encabezado más grande
                            col.Item().Text("Especialista en: " + consulta.NombreEspecialidad).FontSize(12);
                            col.Item().Text("e-mail: " + consulta.EmailMedico).FontSize(11);
                            col.Item().Text("Teléfonos: " + consulta.TelefonoMedico).FontSize(11);
                            col.Item().PaddingTop(2).Text("").FontSize(11);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("Dr:" + consulta.NombresMedico + consulta.ApellidosMedico).Bold().FontSize(14); // Encabezado más grande
                            col.Item().Text("Especialista en: " + consulta.NombreEspecialidad).FontSize(12);
                            col.Item().Text("e-mail: " + consulta.EmailMedico).FontSize(11);
                            col.Item().Text("Teléfonos: " + consulta.TelefonoMedico).FontSize(11);
                            col.Item().PaddingTop(2).Text("").FontSize(11);
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });
                    });


                    page.Content().Column(content =>
                    {
                        // Primera fila
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(275).Column(col =>
                            {
                                col.Item().PaddingTop(10).Text("Fecha: " + consulta.FechaCreacion).FontSize(11).Bold();
                                col.Item().Text("Apellidos: " + consulta.PrimerApellidoPaciente + consulta.SegundoApellidoPaciente).FontSize(11).Bold();
                                col.Item().Text("Nombres: " + consulta.PrimerNombrePaciente + consulta.SegundoNombrePaciente).FontSize(11).Bold();
                                col.Item().Text("Edad: " + consulta.EdadPacientes).FontSize(11).Bold();
                                col.Item().Text("Diagnóstico: " + string.Join(", ", consulta.Diagnosticos.Select(d => d.NombreDiagnostico))).FontSize(11).Bold();


                            });

                            row.ConstantItem(275).Column(col =>
                            {
                                col.Item().PaddingTop(10).Text("Receta: " + consulta.Medicamentos).Bold().FontSize(14); // Encabezado más grande
                                col.Item().Text("CC: " + consulta.CiPacientes).FontSize(12);

                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(275).Column(col =>
                            {
                                col.Item().PaddingTop(10).Text("Fecha: " + consulta.FechaCreacion).FontSize(11).Bold();
                                col.Item().Text("Apellidos: " + consulta.PrimerApellidoPaciente + consulta.SegundoApellidoPaciente).FontSize(11).Bold();
                                col.Item().Text("Nombres: " + consulta.PrimerNombrePaciente + consulta.SegundoNombrePaciente).FontSize(11).Bold();
                                col.Item().Text("Edad: " + consulta.EdadPacientes).FontSize(11).Bold();
                                col.Item().Text("Diagnóstico: " + string.Join(", ", consulta.Diagnosticos.Select(d => d.NombreDiagnostico))).FontSize(11).Bold();

                            });

                            row.ConstantItem(275).Column(col =>
                            {
                                col.Item().PaddingTop(10).Text("Receta: " + consulta.Medicamentos).Bold().FontSize(14); // Encabezado más grande
                                col.Item().Text("CC: " + consulta.CiPacientes).FontSize(12);
                            });
                        });

                        // Segunda fila (Nueva fila)
                        content.Item().Row(row =>
                        {
                            row.ConstantItem(530).PaddingTop(70).Column(col =>
                            {
                                col.Item().Text("IMÁGENES").Bold().FontSize(14); // Título
                                                                                 // Recorre la lista de imágenes y muestra su información
                                foreach (var imagen in consulta.Imagenes)
                                {
                                    col.Item().Text(imagen.NombreImagen).FontSize(12);
                                }
                            });

                            row.ConstantItem(20).PaddingTop(50).Column(col =>
                            {
                                col.Item().Text("").FontSize(12);
                                // Muestra la cantidad total de imágenes
                                var totalCantidad = consulta.Imagenes.Sum(i => i.CantidadImagen);
                                col.Item().Text("x " + totalCantidad).FontSize(12);
                            });

                            row.AutoItem().PaddingHorizontal(10).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.ConstantItem(550).PaddingTop(50).Column(col =>
                            {
                                col.Item().PaddingBottom(20).Text("OBSERVACIONES").Bold().FontSize(14); // Título
                                                                                                        // Recorre la lista de imágenes y muestra las observaciones
                                foreach (var imagen in consulta.Imagenes)
                                {
                                    col.Item().MinWidth(300).Text(imagen.ObserImagen).FontSize(12);
                                }

                                // Si hay espacio adicional, puedes agregar más celdas vacías si es necesario
                                for (int i = consulta.Imagenes.Count; i < 12; i++) // Por ejemplo, llenar hasta 12 celdas
                                {
                                    col.Item().MinWidth(300).Text("").FontSize(12);
                                }
                            });
                        });


                    });

                    page.Footer().Column(footer =>
                    {// Nueva fila del footer


                        // Primera fila del footer
                        footer.Item().Row(row =>
                        {
                            row.ConstantItem(530).Column(col =>
                            {
                                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().Text("Direccion: Av de la Prensa N49-180 y Juan Holguín Edifico Amafkar sexto piso, Chordeleg - Ecuador").FontSize(11).AlignCenter();
                            });

                            row.AutoItem().PaddingHorizontal(30).LineVertical(1).LineColor(Colors.Grey.Lighten1);

                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().Text("Direccion: Av de la Prensa N49-180 y Juan Holguín Edifico Amafkar sexto piso, Chordeleg - Ecuador").FontSize(11).AlignCenter();
                            });
                        });


                    });

                });
            });
        }

        [HttpGet("Buscar")]
        public async Task<IActionResult> BuscarPacientes(int? cedula, string primerNombre, string primerApellido)
        {
            if (cedula == null && string.IsNullOrEmpty(primerNombre) && string.IsNullOrEmpty(primerApellido))
            {
                return BadRequest("Debe proporcionar al menos un criterio de búsqueda: cédula, primer nombre o primer apellido.");
            }

            var pacientes = await _consultationService.BuscarPacientesAsync(cedula, primerNombre, primerApellido);

            if (pacientes == null || !pacientes.Any())
            {
                return NotFound("No se encontraron pacientes con los criterios proporcionados.");
            }

            return Ok(pacientes);
        }



    }

    // DTO (Data Transfer Object) que se espera recibir en la solicitud HTTP
    public class ConsultationDto
    {
        public string UsuarioCreacion { get; set; }
        public string Historial { get; set; }
        public int PacienteId { get; set; }
        public string Motivo { get; set; }
        public string Enfermedad { get; set; }
        public string NombrePariente { get; set; }
        public string SignosAlarma { get; set; }
        public string ReconoFarmacologicas { get; set; }
        public int TipoPariente { get; set; }
        public string TelefonoPariente { get; set; }
        public string Temperatura { get; set; }
        public string FrecuenciaRespiratoria { get; set; }
        public string PresionArterialSistolica { get; set; }
        public string PresionArterialDiastolica { get; set; }
        public string Pulso { get; set; }
        public string Peso { get; set; }
        public string Talla { get; set; }
        public string PlanTratamiento { get; set; }
        public string Observacion { get; set; }
        public string AntecedentesPersonales { get; set; }
        public int DiasIncapacidad { get; set; }
        public int MedicoId { get; set; }
        public int EspecialidadId { get; set; }
        public int TipoConsulta { get; set; }
        public string NotasEvolucion { get; set; }
        public string ConsultaPrincipal { get; set; }
        public int EstadoConsulta { get; set; }

        // Otros parámetros adicionales (organos_sistemas, examen_fisico, etc.)
        public bool OrgSentidos { get; set; }
        public string ObserOrgSentidos { get; set; }
        public bool Respiratorio { get; set; }
        public string ObserRespiratorio { get; set; }
        public bool CardioVascular { get; set; }
        public string ObserCardioVascular { get; set; }
        public bool Digestivo { get; set; }
        public string ObserDigestivo { get; set; }
        public bool Genital { get; set; }
        public string ObserGenital { get; set; }
        public bool Urinario { get; set; }
        public string ObserUrinario { get; set; }
        public bool MEsqueletico { get; set; }
        public string ObserMEsqueletico { get; set; }
        public bool Endocrino { get; set; }
        public string ObserEndocrino { get; set; }
        public bool Linfatico { get; set; }
        public string ObserLinfatico { get; set; }
        public bool Nervioso { get; set; }
        public string ObserNervioso { get; set; }

        // Parámetros para examen físico
        public bool Cabeza { get; set; }
        public string ObserCabeza { get; set; }
        public bool Cuello { get; set; }
        public string ObserCuello { get; set; }
        public bool Torax { get; set; }
        public string ObserTorax { get; set; }
        public bool Abdomen { get; set; }
        public string ObserAbdomen { get; set; }
        public bool Pelvis { get; set; }
        public string ObserPelvis { get; set; }
        public bool Extremidades { get; set; }
        public string ObserExtremidades { get; set; }

        // Parámetros tipo tabla (listas de objetos)
        public List<ConsultaAlergia> Alergias { get; set; }
        public List<ConsultaCirugia> Cirugias { get; set; }
        public List<ConsultaMedicamento> Medicamentos { get; set; }
        public List<ConsultaLaboratorio> Laboratorio { get; set; }
        public List<ConsultaImagen> Imagenes { get; set; }
        public List<ConsultaDiagnostico> Diagnosticos { get; set; }
    }
}
