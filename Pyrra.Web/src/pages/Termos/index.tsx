import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'

// Página pública (fora do guard de sessão): precisa ser legível antes do
// cadastro, já que é no Cadastro que o usuário declara o aceite. Segue o mesmo
// shell das telas de auth (wordmark + container centralizado), mas com largura
// maior (max-w-lg) por ser texto corrido, e sem centralização vertical.

// Data da última revisão destes termos. Constante única para o cabeçalho não
// divergir de eventuais referências futuras.
const LAST_UPDATED = '26 de julho de 2026'

// Título de seção numerada + corpo. Mantém o espaçamento uniforme entre as 12
// seções sem repetir as mesmas classes em cada uma.
function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <h2 className="font-display text-lg font-semibold tracking-tight text-ink">
        {title}
      </h2>
      {children}
    </section>
  )
}

function Paragraph({ children }: { children: ReactNode }) {
  return <p className="text-sm leading-relaxed text-slate-300">{children}</p>
}

// Lista com marcador, no mesmo tom secundário dos parágrafos.
function List({ items }: { items: ReactNode[] }) {
  return (
    <ul className="flex list-disc flex-col gap-1.5 pl-5 text-sm leading-relaxed text-slate-300 marker:text-slate-600">
      {items.map((item, index) => (
        <li key={index}>{item}</li>
      ))}
    </ul>
  )
}

export function Termos() {
  return (
    <main className="flex min-h-screen flex-col items-center px-4 py-12">
      <div className="w-full max-w-lg">
        <header className="mb-10 text-center">
          <h1 className="font-display text-5xl font-semibold tracking-tight text-ink">
            Pyrra
          </h1>
          <p className="mt-2 text-sm text-slate-400">Termos de Uso</p>
          <p className="mt-1 text-xs text-slate-500">
            Última atualização: {LAST_UPDATED}
          </p>
        </header>

        <div className="flex flex-col gap-8">
          <Section title="1. Sobre este documento">
            <Paragraph>
              Estes Termos de Uso regulam o acesso e uso do aplicativo Pyrra
              ("Pyrra", "nós", "nosso"), disponível em pyrra.com.br. Ao criar uma
              conta ou usar o Pyrra, você ("usuário", "você") concorda
              integralmente com estes termos. Se não concordar, não utilize o app.
            </Paragraph>
          </Section>

          <Section title="2. O que é o Pyrra">
            <Paragraph>
              O Pyrra é uma aplicação pessoal de acompanhamento de hábitos,
              tarefas, treino, nutrição, finanças pessoais e reflexões diárias, com
              um assistente de inteligência artificial (Zelo) integrado. O Pyrra
              está atualmente em fase de desenvolvimento ativo (MVP/beta), o que
              significa que:
            </Paragraph>
            <List
              items={[
                'Funcionalidades podem mudar, ser adicionadas ou removidas sem aviso prévio;',
                'Podem ocorrer instabilidades, bugs ou indisponibilidades temporárias;',
                'O serviço é gratuito nesta fase e não há garantia de que permanecerá gratuito indefinidamente (mudanças futuras de modelo serão comunicadas com antecedência).',
              ]}
            />
          </Section>

          <Section title="3. Cadastro e elegibilidade">
            <Paragraph>
              Para usar o Pyrra, você deve criar uma conta com nome, email e senha
              válidos. Você é responsável por:
            </Paragraph>
            <List
              items={[
                'Manter suas credenciais em sigilo;',
                'Todas as atividades realizadas na sua conta;',
                'Fornecer informações verdadeiras no cadastro.',
              ]}
            />
            <Paragraph>
              Não há restrição mínima de idade definida formalmente nesta fase, mas
              o Pyrra não foi desenhado especificamente para crianças. Se você é
              menor de 18 anos, recomenda-se o uso com supervisão de um responsável.
            </Paragraph>
          </Section>

          <Section title="4. Dados que coletamos e como usamos (LGPD)">
            <Paragraph>
              Em conformidade com a Lei Geral de Proteção de Dados (Lei
              13.709/2018), informamos que o Pyrra coleta e processa os seguintes
              dados pessoais:
            </Paragraph>
            <List
              items={[
                'Dados de cadastro: nome, email, senha (armazenada de forma criptografada);',
                'Dados de uso: hábitos e tarefas cadastrados, registros de treino, plano nutricional, lançamentos financeiros manuais, entradas de diário/reflexão, e histórico de conversas com o assistente Zelo.',
              ]}
            />
            <Paragraph>
              Esses dados são usados exclusivamente para fornecer e melhorar o
              funcionamento do app. Não vendemos, alugamos ou compartilhamos seus
              dados pessoais com terceiros para fins de marketing. Dados podem ser
              processados por provedores de infraestrutura (hospedagem, banco de
              dados, e o modelo de IA usado pelo Zelo) estritamente para operação do
              serviço.
            </Paragraph>
            <Paragraph>
              Você pode, a qualquer momento, solicitar a exclusão da sua conta e dos
              dados associados entrando em contato através dos canais informados na
              seção 10.
            </Paragraph>
          </Section>

          <Section title="5. Uso aceitável">
            <Paragraph>Ao usar o Pyrra, você concorda em não:</Paragraph>
            <List
              items={[
                'Utilizar o app para fins ilegais ou fraudulentos;',
                'Tentar acessar contas, dados ou sistemas de outros usuários sem autorização;',
                'Tentar comprometer a segurança, disponibilidade ou integridade da infraestrutura do Pyrra (incluindo ataques de negação de serviço, exploração de vulnerabilidades, engenharia reversa do código);',
                'Automatizar o uso do app (bots, scraping) sem autorização prévia;',
                'Utilizar o assistente Zelo para gerar conteúdo ilegal, ofensivo ou prejudicial.',
              ]}
            />
            <Paragraph>
              Violações podem resultar na suspensão ou encerramento da sua conta, a
              critério do Pyrra.
            </Paragraph>
          </Section>

          <Section title="6. Propriedade intelectual">
            <Paragraph>
              O código-fonte, design, marca e demais elementos do Pyrra são de
              propriedade do desenvolvedor do projeto. Os dados que você insere no
              app (seus hábitos, tarefas, reflexões, etc.) continuam sendo seus,
              você mantém total propriedade sobre o conteúdo que cria.
            </Paragraph>
          </Section>

          <Section title="7. Assistente Zelo (Inteligência Artificial)">
            <Paragraph>
              O Zelo é um assistente baseado em modelo de linguagem (IA). Você
              reconhece que:
            </Paragraph>
            <List
              items={[
                'As respostas do Zelo podem conter imprecisões ou erros ("alucinações"), como em qualquer sistema de IA;',
                'O Zelo não substitui aconselhamento profissional médico, nutricional, financeiro, psicológico ou de qualquer outra natureza especializada;',
                'O uso das sugestões do Zelo é de sua inteira responsabilidade.',
                'Há um limite diário de interações com o Zelo, que pode ser ajustado a qualquer momento.',
              ]}
            />
          </Section>

          <Section title="8. Limitação de responsabilidade">
            <Paragraph>
              O Pyrra é fornecido "como está" ("as is"), sem garantias de
              disponibilidade contínua, ausência de erros, ou adequação a uma
              finalidade específica. Na máxima extensão permitida pela lei:
            </Paragraph>
            <List
              items={[
                'Não nos responsabilizamos por perdas de dados decorrentes de falhas técnicas, especialmente durante esta fase de desenvolvimento ativo;',
                'Não nos responsabilizamos por decisões pessoais, financeiras, de saúde ou nutricionais tomadas com base em informações do app ou do Zelo;',
                'Recomendamos não depender do Pyrra como único registro de informações críticas.',
              ]}
            />
          </Section>

          <Section title="9. Alterações nestes termos">
            <Paragraph>
              Podemos atualizar estes Termos de Uso conforme o Pyrra evolui.
              Mudanças significativas serão comunicadas dentro do próprio
              aplicativo. O uso continuado do app após uma atualização implica
              concordância com os novos termos.
            </Paragraph>
          </Section>

          <Section title="10. Encerramento de conta">
            <Paragraph>
              Você pode encerrar sua conta a qualquer momento entrando em contato
              conosco. Reservamo-nos o direito de suspender ou encerrar contas que
              violem estes termos.
            </Paragraph>
          </Section>

          <Section title="11. Legislação aplicável">
            <Paragraph>
              Estes termos são regidos pelas leis da República Federativa do Brasil.
              Eventuais disputas serão submetidas ao foro da comarca de Curitiba/PR.
            </Paragraph>
          </Section>

          <Section title="12. Contato">
            <Paragraph>
              Dúvidas, solicitações de exclusão de dados, ou qualquer outra questão
              relacionada a estes termos podem ser enviadas para:{' '}
              <a
                href="mailto:contato@pyrra.com.br"
                className="font-medium text-brand-green transition hover:brightness-110"
              >
                contato@pyrra.com.br
              </a>
              .
            </Paragraph>
          </Section>
        </div>

        {/* Volta ao cadastro: quem chega aqui vindo do aceite deve conseguir
            retornar sem depender do "voltar" do navegador. */}
        <p className="mt-10 text-center text-sm text-slate-400">
          <Link
            to="/cadastro"
            className="inline-flex items-center gap-1.5 font-medium text-brand-green transition hover:brightness-110"
          >
            <ArrowLeft size={15} aria-hidden="true" />
            Voltar ao cadastro
          </Link>
        </p>
      </div>
    </main>
  )
}

export default Termos
