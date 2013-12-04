select SF.os_file_name, SF.akisim_file_name FROM source_files SF, repo_source RS, repo_branch RB 
Where RB.repo_short_name = "akisim" AND RB.branch_name = "feature/simplifying" 
AND RB.idrepo_branch = RS.idrepo
AND RS.idsource = SF.index;

