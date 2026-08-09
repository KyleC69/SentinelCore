@echo off
@echo This cmd file creates a Data API Builder configuration based on the chosen database objects.
@echo To run the cmd, create an .env file with the following contents:
@echo dab-connection-string=your connection string
@echo ** Make sure to exclude the .env file from source control **
@echo **
dotnet tool install -g Microsoft.DataApiBuilder --prerelease
dab init -c dab-config.json --database-type mssql --connection-string "@env('dab-connection-string')" --host-mode Development
@echo Adding tables
dab add "CaseEntity" --source "[dbo].[CaseEntity]" --fields.include "Id,CaseRecordId,CaseId,CreatedAt,PlanId,Status,InitiatingSignal,UpdatedAt" --permissions "anonymous:*" 
dab add "EvidenceEntity" --source "[dbo].[EvidenceEntity]" --fields.include "Id,EvidenceId,CaseRecordId,ContentJson,Provenance,Source,Timestamp,Type" --permissions "anonymous:*" 
dab add "InvestigationPlanEntity" --source "[dbo].[InvestigationPlanEntity]" --fields.include "Id,PlanId,CreatedAt" --permissions "anonymous:*" 
dab add "InvestigationPlanStepsEntity" --source "[dbo].[InvestigationPlanStepsEntity]" --fields.include "Id,StepId,PlanId,Surface,Instruction,Result,CompletedSuccessfully,TaskBlocked,IsTargetPropertyMissing" --permissions "anonymous:*" 
dab add "PatternMemoryEntity" --source "[dbo].[PatternMemoryEntity]" --fields.include "Id,PatternId,CaseId,SignalEmbedding,SummaryEmbedding,Summary,Timestamp" --permissions "anonymous:*" 
dab add "ResolutionEntity" --source "[dbo].[ResolutionEntity]" --fields.include "Id,RawJsonContent,CaseRecordId,Notes,CreatedAt,Verified" --permissions "anonymous:*" 
dab add "SignalEntity" --source "[dbo].[SignalEntity]" --fields.include "Id,SignalId,CaseRecordId,SignalText,Source,Timestamp" --permissions "anonymous:*" 
@echo Adding views and tables without primary key
@echo Adding column descriptions
dab update InvestigationPlanStepsEntity --fields.StepId "StepId" --fields.description "Links this step the plan it was created in"
dab update InvestigationPlanStepsEntity --fields.Surface "Surface" --fields.description "Domain or Surface that the task applies to"
dab update InvestigationPlanStepsEntity --fields.IsTargetPropertyMissing "IsTargetPropertyMissing" --fields.description "If the target of the task is not found this bit must be flipped"
@echo Adding relationships
dab update CaseEntity --relationship InvestigationPlanEntity --target.entity InvestigationPlanEntity --cardinality one
dab update InvestigationPlanEntity --relationship CaseEntity --target.entity CaseEntity --cardinality many
dab update EvidenceEntity --relationship CaseEntity --target.entity CaseEntity --cardinality one
dab update CaseEntity --relationship EvidenceEntity --target.entity EvidenceEntity --cardinality many
dab update InvestigationPlanStepsEntity --relationship InvestigationPlanEntity --target.entity InvestigationPlanEntity --cardinality one
dab update InvestigationPlanEntity --relationship InvestigationPlanStepsEntity --target.entity InvestigationPlanStepsEntity --cardinality many
dab update SignalEntity --relationship CaseEntity --target.entity CaseEntity --cardinality one
dab update CaseEntity --relationship SignalEntity --target.entity SignalEntity --cardinality many
@echo Adding stored procedures
@echo **
@echo ** run 'dab validate' to validate your configuration **
@echo ** run 'dab start' to start the development API host **
