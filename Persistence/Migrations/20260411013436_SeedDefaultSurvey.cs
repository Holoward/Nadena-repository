using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultSurvey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO SurveyTemplates (ResearcherId, Title, Description, IsActive, Version, Created, CreatedBy, LastModified, LastModifiedBY)
                VALUES ('system', 'Media Consumption and Attitudes', 'This short survey helps researchers understand the context behind your viewing habits. It takes about 3 minutes. Your answers are linked to your viewing data by an anonymous ID only.', 1, '1.0', datetime('now'), 'System', NULL, NULL);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO SurveyQuestions (SurveyTemplateId, OrderIndex, QuestionText, QuestionType, Options, ScaleMin, ScaleMax, ScaleMinLabel, ScaleMaxLabel, IsRequired, Created, CreatedBy, LastModified, LastModifiedBY)
                VALUES 
                (1, 0, 'How often do you watch YouTube in a typical week?', 0, '[""Less than 1 hour"", ""1-3 hours"", ""3-7 hours"", ""7-14 hours"", ""More than 14 hours""]', NULL, NULL, NULL, NULL, 1, datetime('now'), 'System', NULL, NULL),
                (1, 1, 'Which of the following best describes why you use YouTube?', 1, '[""Entertainment"", ""Learning / tutorials"", ""News and current events"", ""Music"", ""Following specific creators"", ""Background noise"", ""Other""]', NULL, NULL, NULL, NULL, 1, datetime('now'), 'System', NULL, NULL),
                (1, 2, 'How much do you trust the news content you see on YouTube?', 2, NULL, 1, 7, 'Do not trust at all', 'Trust completely', 1, datetime('now'), 'System', NULL, NULL),
                (1, 3, 'How often do you click on YouTube''s recommended videos rather than searching for something specific?', 0, '[""Almost always"", ""More often than not"", ""About half the time"", ""Less often than not"", ""Almost never""]', NULL, NULL, NULL, NULL, 1, datetime('now'), 'System', NULL, NULL),
                (1, 4, 'Is there anything about your YouTube usage you would like researchers to know that the data might not capture?', 3, NULL, NULL, NULL, NULL, NULL, 0, datetime('now'), 'System', NULL, NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM SurveyQuestions WHERE SurveyTemplateId = 1;");
            migrationBuilder.Sql("DELETE FROM SurveyTemplates WHERE Id = 1;");
        }
    }
}