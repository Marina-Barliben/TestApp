using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace WebApplication3.Models
{
    public abstract class Answer
    {
        public int Id { get; set; }
        [Required]
        public Question Question { get; set; }
        [Required]
        public string AnswerType { get; set; }
        [Required]
        public float Score { get; set; }
        [Required]
        public TestResult TestResult { get; set; }
        [Required]
        public ushort Order { get; set; }
        
        public List<Option> Options { get; set; }
    }
    
    public  class SingleChoiceAnswer : Answer
    {
        public Option Option { get; set; }
    }
    
    public  class MultiChoiceAnswer : Answer
    {
    }
    
    public  class TextAnswer : Answer
    {
        public string Text { get; set; }
    }
    public  class DragAndDropAnswer : Answer
    {
        public List<DragAndDropAnswerOption> DragAndDropAnswerOptions { get; set; }
    }

    public class DragAndDropAnswerOption
    {
        public int Id { get; set; }
        [Required]
        public Option RightOption { get; set; }
        public int ChosenOrder { get; set; }
    }
}