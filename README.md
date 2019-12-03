# Baichador

  O Setup está localizado na pasta *Release*.  
  O programa Baichador faz o download de músicas do YouTube e as salva no formato mp3, utilizando a ferramenta *youtube-dl* em conjunto com *ffmpeg*, que são automaticamente instalados junto com o programa.  
  O programa tem uma série de configurações no arquivo *Baichador.config* que fica na mesma pasta do executável. Você pode editar essas configurações para fazer uma breve customizada no programa.  
  O programa fornece 3 opções de download:  
  
## Download de uma única música   
  O programa irá fazer o download de apenas uma música e salvá-la com um nome customizado e opcional. O arquivo será salvo na pasta padrão (Música). É obrigatório fornecer a URL da música, e você pode definir o nome do arquivo .mp3 final (opcional). Se você não colocar nada nesse campo, o nome do arquivo será o título da música no YouTube. (Obs: você pode também especificar o diretório em que você deseja salvar o arquivo, basta colocar o caminho, ex: *pasta\que\vcquesalvar\nomedoarquivo*  Lembrando que essa pasta é em relação à pasta Música, então não adianta colocar C:\ que não vai funcionar. Caso queira fazer isso, edite o arquivo .config e mude a pasta padrão)  
  
## Download de várias músicas (por arquivo)  
  O programa irá fazer o download de várias músicas especificadas em um arquivo. É obrigatório fornecer o arquivo, e você pode definir a pasta em que as músicas serão salvas (opcional). Se você não colocar nada nesse campo, as músicas serão salvas na pasta padrão (Música). No arquivo, as músicas deverão ser colocadas uma por linha, onde cada linha tem a URL da música, seguida de um espaço e o nome do arquivo .mp3 final, sendo esse nome opcional (o nome do arquivo pode conter espaços). Caso você não especificar o nome, este será o título da música no YouTube.  
  Exemplo de arquivo válido:  
  
>   URL1 um nome aqui  
>   URL2  
>   URL3 arquivo.mp3  
  
## Download de uma playlist  
  O programa irá fazer o download de uma playlist. É obrigatório fornecer a URL da playlist, e você pode definir a pasta em que as músicas serão salvas (opcional). Se você não colocar nada nesse campo, as músicas serão salvas na pasta padrão (Música).  
  
  
## CASO NÃO CONSIGA BAIXAR NENHUMA MÚSICA  
  1) Verfique se você está conectado à internet, se as urls estão corretas etc.  
  2) Verique se o *youtube-dl* está atualizado. Para isto, basta clicar no botão "Atualizar" que o programa será atualizado automaticamente.  
  3) Caso você não consiga atualizar, execute o programa com privilégios de administrador.  
  4) Verifique se o pacote Microsoft Visual C++ 2010 Redistributable Package (x86) está instalado. Se não estiver, instale-o.  
  5) Caso o error persistir, contate o desenvolvedor lindo e maravilhoso.  
  
Aproveite o programa :)  
=======
# baichador
Um programa que baixa músicas do YouTube
