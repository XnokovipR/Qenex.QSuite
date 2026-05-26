# QENEX

QENEX je komplexní software pro komunikaci s různými řídícími jednotkami nebo počítači (obecně elektronickými zařízeními) v reálném čase.
Základni verze může běžet jako služba na Linuxu, Windows, případně MacOS (na MacOS nevyzkoušeno).
Qenex aplikace je tvořena záklaním modulem, který obsahuje následující části:
 - proměnné: jsou to proměnné, které se komunikují s externími zařízeními.
 - ovladače (drivery): knihovny umožňující komunikaci s externími zařízeními.
 - protokoly: komunikace se zařízeními je prováděna pomocí protokolů.
 - skripty: python skripty jsou programy, které mohou reagovat na přijatá data ze zařízení.
            Skripty se mohou spouštět při startu, stopu, periocicky, nebo při změně stavu proměnných.
            Proměnné vytvořené ve sKriptech spoustěnýché při startu jsou sdíleny mezi všemi skripty.
            Skripty vidí aktuální stavy komunikovaných proměnných, přistupují k nim přes JmenoPromenne.Value,
            JmenoPromenne.Name, JmenoPromenne.Id.

Tyto informace jsou uloženy do konfiguračního souboru, 
který je při spuštění aplikace načten a použit pro nastavení aplikace.
Aplikace pak může být spuštěna v konzoli nebo jako služba.

Nadstavbou nad tímto modulem je Qenex-GUI nazývané QInsight, 
které umožňuje snadnou konfiguraci a správu aplikace.
Má navíc pracovní plochy, kde je možné přidávat kontrolery jako Grafy, zobrazení hodnoty komunikovaného signalu, apod.
Ukládát data do logu, přehrávat data z logu apod. - viz. grafická ukázka.
