
# Pacote Movimento

Pacote qualquer jogo controlado por um dispositivo com giroscópio que considere 5 posições: 

* cima
* baixo
* centro
* esquerda
* direita

Essas poderiam ser 5 movimentos do pé, da mão, do braço, etc.

Para usar, importe este repositório inteiro para a pasta assets e importe o prefab `Sensores` para a sua cena inicial. Esse objeto irá persistir entre cenas, e manter a conexão com o arduino.

Suba no arduino o script [arduino.ino](arduino/arduino.ino), mudando o nome do dispositivo na variável `identificador`.

## Novos dispositivos

Se você for desenvolver um novo dispositivo compatível, abra o prefab dos sensores no Inspetor do Unity e em `Gerenciador Movimentos` incremente o `Dicionario Nomes Por Dispositivo`, para adicionar os nomes dos movimentos. 

Na pasta [sprites/tutorial/](sprites/tutorial/), você pode adicionar imagens com os nomes `top`,`bot`,`mid`,`right` e `left` para que sejam mostradas no tutorial desse novo dispositivo.

Para uso, como eu ainda não escrevi um tutorial decente, sugiro que você compare com o cruzamentoPapete e o projetoPacman.