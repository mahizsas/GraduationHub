﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using GraduationHub.Web.Data;
using GraduationHub.Web.Domain;
using GraduationHub.Web.Infrastructure.Mapping;

namespace GraduationHub.Web.Models.FrequentlyAskedQuestions
{
    public class FrequentlyAskedQuestionEditModel : IMapFrom<FrequentlyAskedQuestion>
    {
        [HiddenInput]
        public int Id { get; set; }

        [Range(1, 20)]
        public int Order { get; set; }

        [Required, StringLength(FieldLengths.FrequentlyAskedQuestion.Question)]
        public string Question { get; set; }

        [Required, StringLength(FieldLengths.FrequentlyAskedQuestion.Answer),
            DataType(DataType.MultilineText), AllowHtml]
        public string Answer { get; set; } 
    }
}