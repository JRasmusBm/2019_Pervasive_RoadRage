nodemon -x "pdflatex --shell-escape main; bibtex main; pdflatex
--shell-escape main; pdflatex
--shell-escape main" -e "tex,bib" 
