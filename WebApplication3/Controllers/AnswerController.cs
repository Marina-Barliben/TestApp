﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Models.AnswerViewModels;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using Microsoft.CodeAnalysis.Emit;

namespace WebApplication3.Controllers
{
    public class AnswerController : Controller
    {
        #region Поля
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        private readonly SignInManager<User> _signInManager;

        //private readonly IEmailSender _emailSender;
        //private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        #endregion

        #region Конструктор
        public AnswerController(

            ApplicationDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            // IEmailSender emailSender,
            //ISmsSender smsSender,
            ILoggerFactory loggerFactory
        )
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            //_emailSender = emailSender;
            //_smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<UserController>();
        }
        #endregion

        #region Входная точка
        [Authorize]
        [HttpGet]
        [Route("/{testResultId}/Question/{answerId}/")]
        public async Task<IActionResult> Answer(int testResultId, ushort answerId)
        {
            var testResult = await _context.TestResults
                .Include(tr => tr.Answers)
            .SingleAsync(tr => tr.Id == testResultId);
            if (testResult == null) return NotFound();
            var answers = testResult.Answers.OrderBy(a => a.Order).ToList();

            return View("Answer", answers);
        }
        #endregion

        #region GET
        [Authorize]
        [HttpGet]
        [Route("/SingleChoiceAnswer/{answerId}")]
        public async Task<IActionResult> LoadSingleChoiceAnswer(int answerId)
        {
            var answer = await _context.SingleChoiceAnswers
                    .Include(a => a.TestResult)
                        .ThenInclude(tr => tr.Test)
                    .Include(a => a.Question)
                        .ThenInclude(q => q.Options)
                .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            return PartialView("_LoadSingleChoiceAnswer", answer);
        }

        [Authorize]
        [HttpGet]
        [Route("/TextAnswer/{answerId}")]
        public async Task<IActionResult> LoadTextAnswer(int answerId)
        {
            var answer = await _context.TextAnswers
                    .Include(a => a.TestResult)
                        .ThenInclude(tr => tr.Test)
                    .Include(a => a.Question)
                .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            return PartialView("_LoadTextAnswer", answer);
        }

        [Authorize]
        [HttpGet]
        [Route("/MultiChoiceAnswer/{answerId}")]
        public async Task<IActionResult> LoadMultiChoiceAnswer(int answerId)
        {
            var answer = await _context.MultiChoiceAnswers
                    .Include(a => a.TestResult)
                        .ThenInclude(tr => tr.Test)
                    .Include(a => a.AnswerOptions)
                        .ThenInclude(ao => ao.Option)
                    .Include(a => a.Question)
                        .ThenInclude(q => q.Options)
                .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            var checkedOptionIds = new List<int>();
            var rightOptionIds = new List<int>();
            if (answer.AnswerOptions != null)
            {
                foreach (var answerOption in answer.AnswerOptions)
                {
                    if (answerOption.Checked) checkedOptionIds.Add(answerOption.OptionId);
                    if (answerOption.Option.IsRight) rightOptionIds.Add(answerOption.OptionId);
                }
            };
            ViewBag.checkedOptionsIds = checkedOptionIds;
            ViewBag.rightOptionsIds = rightOptionIds;
            return PartialView("_LoadMultiChoiceAnswer", answer);
        }

        [Authorize]
        [HttpGet]
        [Route("/DragAndDropAnswer/{answerId}")]
        public async Task<IActionResult> LoadDragAndDropAnswer(int answerId)
        {
            var answer = await _context.DragAndDropAnswers
                    .Include(a => a.TestResult)
                        .ThenInclude(tr => tr.Test)
                    .Include(a => a.DragAndDropAnswerOptions)
                        .ThenInclude(o => o.RightOption)
                    .Include(a => a.Question)
                        .ThenInclude(q => q.Options)
                .SingleAsync(a => a.Id == answerId)
                ;
            Shuffle(answer.Question.Options);
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }
            return PartialView("_LoadDragAndDropAnswer", answer);
        }

        [Authorize]
        [HttpGet]
        [Route("/{testResultId}/Results/Question/{answerId}/")]
        public async Task<IActionResult> AnswerResults(int testResultId, ushort answerId)
        {
            var testResult = await _context.TestResults
                .Include(tr => tr.Answers)
                .SingleAsync(tr => tr.Id == testResultId);
            if (testResult == null) return NotFound();
            var answers = testResult.Answers.OrderBy(a => a.Order).ToList();

            return View("AnswerResults", answers);
        }

        [Authorize]
        [HttpGet]
        [Route("/{testResultId}/DetailedResults/Question/<answerOrder>/")]
        public async Task<IActionResult> AnswerDetailedResults(int testResultId, ushort answerOrder)
        {
            var testResult = await _context.TestResults
                .Include(tr => tr.Answers)
                .SingleAsync(tr => tr.Id == testResultId);
            if (testResult == null) return NotFound();


            return View("AnswerResults", testResult.Answers);
        }

        [Authorize]
        [HttpGet]
        [Route("/CodeAnswer/{answerId}")]
        public async Task<IActionResult> LoadCodeAnswer(int answerId)
        {
            var answer = await _context.CodeAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Code)
                    .Include(a => a.Question)
                        .ThenInclude(q => q.Options)
                .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            return PartialView("_LoadCodeAnswer", answer);
        }
        #endregion

        #region POST
        [Authorize]
        [HttpPost]
        [Route("/SingleChoiceAnswer/{answerId}/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SingleChoiceAnswer(int answerId, [FromBody]SingleChoiceAnswerViewModel model)
        {

            var answer = await _context.SingleChoiceAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Question)
                    .SingleAsync(a => a.Id == answerId)
                ;
            if (model.OptionId == 0) return new JsonResult("");
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            //проверить что пользоавтель может проходить тест
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            var option = await _context.Options.SingleAsync(o => o.Id == model.OptionId);
            // проверить что опшн принадлежит к вопросу
            if (!answer.Question.Options.Contains(option))
            {
                return BadRequest();
            }


            answer.Option = option;
            _context.Answers.Update(answer);
            await _context.SaveChangesAsync();
            return new JsonResult("");
        }

        [Authorize]
        [HttpPost]
        [Route("/MultiChoiceAnswer/{answerId}/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MultiChoiceAnswer(int answerId, [FromBody]MultiChoiceAnswerViewModel model)
        {


            var answer = await _context.MultiChoiceAnswers
                    .Include(a => a.TestResult).Include(a => a.AnswerOptions)
                    .Include(a => a.Question).ThenInclude(a => a.Options)
                    .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            //проверить что пользоавтель может проходить тест
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            //TODO ошибка
            // проверить что опшнsы принадлежит к вопросу
            foreach (var id in model.CheckedOptionIds)
            {
                if (!answer.Question.Options.Exists(o => o.Id == id))
                {
                    return BadRequest();
                }
            }
            //создать
            if (answer.AnswerOptions.Count == 0)
            {
                foreach (var option in answer.Question.Options)
                {
                    _context.AnswerOptions.Add(new AnswerOption
                    {
                        Answer = answer,
                        AnswerId = answerId,
                        Option = option,
                        OptionId = option.Id,
                        Checked = model.CheckedOptionIds.Contains(option.Id)
                    });
                }
                await _context.SaveChangesAsync();
            }
            // обновить
            else
            {
                foreach (var answerOption in answer.AnswerOptions)
                {
                    answerOption.Checked = model.CheckedOptionIds.Contains(answerOption.OptionId);
                    _context.AnswerOptions.Update(answerOption);
                }
                await _context.SaveChangesAsync();
            }
            _context.Answers.Update(answer);
            await _context.SaveChangesAsync();
            return new JsonResult("");
        }

        [Authorize]
        [HttpPost]
        [Route("/TextAnswer/{answerId}/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TextAnswer(int answerId, [FromBody]TextAnswerViewModel model)
        {

            var answer = await _context.TextAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Question)
                    .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            //проверить что пользоавтель может проходить тест
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            answer.Text = model.Text;
            _context.Answers.Update(answer);
            await _context.SaveChangesAsync();
            return new JsonResult("");
        }

        [Authorize]
        [HttpPost]
        [Route("/DragAndDropAnswer/{answerId}/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DragAndDropAnswer(int answerId, [FromBody]DragAndDropAnswerViewModel model)
        {

            var answer = await _context.DragAndDropAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Question)
                    .Include(a => a.DragAndDropAnswerOptions)
                    .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            //проверить что пользоавтель может проходить тест
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }
            foreach (var option in model.Options)
            {
                var optionQ = await _context.Options.SingleAsync(o => o.Id == option.OptionId);
                // проверить что опшн принадлежит к вопросу
                if (!answer.Question.Options.Contains(optionQ))
                {
                    return BadRequest();
                }
            }
            if (answer.DragAndDropAnswerOptions.Count == 0)
            {
                int i = 0;
                foreach (var option in model.Options)
                {
                    var rightOrder = answer.Question.Options.OrderBy(o => o.Order).ToList();
                    var rightOption = rightOrder[i++];
                    _context.DragAndDropAnswerOptions.Add(new DragAndDropAnswerOption
                    {
                        Answer = answer,
                        AnswerId = answerId,
                        RightOption = rightOption,
                        OptionId = option.OptionId,
                        RightOptionId = rightOption.Id,
                        ChosenOrder = option.ChosenOrder
                    });
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                var rightOrder = answer.Question.Options.OrderBy(o => o.Order).ToList();
                foreach (var option in answer.DragAndDropAnswerOptions)
                {
                    option.ChosenOrder = model.Options.Single(o => o.OptionId == option.OptionId).ChosenOrder;
                    option.RightOption = rightOrder[option.ChosenOrder - 1];
                    option.RightOptionId = option.RightOption.Id;
                }
            }
            _context.Answers.Update(answer);
            await _context.SaveChangesAsync();
            return new JsonResult("");
        }

        [Authorize]
        [HttpPost]
        [Route("/CodeAnswer/{answerId}/")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CodeAnswer(int answerId, [FromBody]CodeAnswerViewModel model)
        {

            var answer = await _context.CodeAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Code)
                    .Include(a => a.Question)
                    .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            //проверить что пользоавтель может проходить тест
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }

            var option = await _context.Options.SingleAsync(o => o.Question == answer.Question);
            // проверить что опшн принадлежит к вопросу
            if (!answer.Question.Options.Contains(option))
            {
                return BadRequest();
            }
            var code = await _context.Codes.SingleAsync(c => c.Answer == answer);
            code.Args = model.Code.Args; code.Value = model.Code.Value; code.Output = model.Code.Output;
            _context.Codes.Update(code);
            answer.Code = code;
            answer.Option = option;
            _context.Answers.Update(answer);
            await _context.SaveChangesAsync();
            return new JsonResult("");
        }
        #endregion

        #region Code
        [Authorize]
        [HttpGet]
        [Route("/Code/{answerId}")]
        public async Task<IActionResult> GetCode(int answerId)
        {
            var answer = await _context.CodeAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Question)
                        .ThenInclude(q => q.Options)
                .SingleAsync(a => a.Id == answerId)
                ;
            if (answer == null) return NotFound();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (_context.TestResults.Count(tr => tr.Id == answer.TestResult.Id && tr.CompletedByUser == user) == 0)
            {
                return NotFound();
            }
            Code code;
            try
            {
                code = await _context.Codes.SingleAsync(c => c.Answer == answer);
            }
            catch (Exception)
            {
                using (var ts = _context.Database.BeginTransaction())
                {
                    code = new Code { Output = "Output", Answer = answer };
                    code = (await _context.AddAsync(code)).Entity;
                    await _context.SaveChangesAsync();
                    ts.Commit();
                }
            }
            return PartialView("CodeOutput", code);
        }
        [Authorize]
        [HttpPost]
        [Route("/Code/{answerId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostCode(int answerId, [FromBody]Code model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var answer = await _context.CodeAnswers
                    .Include(a => a.TestResult)
                    .Include(a => a.Question)
                    .SingleAsync(a => a.Id == answerId)
                ;
            Code code;
            try
            {
                code = await _context.Codes.SingleAsync(c => c.Answer == answer);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            code.Value = model.Value;
            code.Args = model.Args;
            object[] args;
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(model.Value);
            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            StringBuilder message = new StringBuilder();
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        message.AppendFormat("{0}: {1}\n", diagnostic.Id, diagnostic.GetMessage());
                        code.Output = message.ToString();
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    Type type = assembly.GetType("TestsApp.Program");
                    object obj = Activator.CreateInstance(type);
                    if (!string.IsNullOrEmpty(model.Args))
                    {
                        var method = type.GetMethod("Main");
                        var parameters = method.GetParameters();
                        args = new object[parameters.Length];
                        List<Type> types = new List<Type>();
                        foreach (var p in parameters)
                        {
                            types.Add(p.ParameterType);
                        }
                        string[] tmp = model.Args.Split(',').Select(arg => arg.Trim()).ToArray();
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            args[i] = Convert.ChangeType(tmp[i], types[i]);
                        }
                    }
                    else
                        args = null;
                    try
                    {
                        code.Output = type.InvokeMember("Main",
                        BindingFlags.Default | BindingFlags.InvokeMethod,
                        null,
                        obj,
                        args).ToString();
                    }
                    catch (Exception e)
                    {
                        code.Output = e.Message;
                    }
                }
            }
            using (var ts = _context.Database.BeginTransaction())
            {
                _context.Codes.Update(code);
                await _context.SaveChangesAsync();
                ts.Commit();
            }
            await _context.SaveChangesAsync();
            return new JsonResult("");
        }
        #endregion

        #region Вспомогательные методы
        private static Random rng = new Random();

        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        #endregion
    }
}